// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Exceptions;
using AutoCrane.Interfaces;

using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal class LeaderElection : ILeaderElection
    {
        private const string LeaderAnnotationName = "control-plane.alpha.kubernetes.io/leader";

        // the leader will refresh a little before it expires
        private static readonly TimeSpan TimeToRefreshBeforeExpiry = TimeSpan.FromSeconds(10);

        // in case there is a concurrent write for leadership
        private static readonly TimeSpan TimeToWaitAfterSettingLeader = TimeSpan.FromSeconds(10);
        private readonly ILogger<LeaderElection> logger;
        private readonly IEndpointAnnotationAccessor client;
        private readonly string ns;
        private readonly string identity;
        private string leaderObjectName = string.Empty;
        private TimeSpan leaseDuration = TimeSpan.FromSeconds(90);

        public LeaderElection(IEndpointAnnotationAccessor client, IAutoCraneConfig config, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<LeaderElection>();
            this.client = client;
            this.ns = config.Namespaces.First();
            this.identity = Environment.MachineName;
            if (!config.IsAllowedNamespace(this.ns))
            {
                throw new ForbiddenException($"namespace: {this.ns}");
            }
        }

        public bool IsLeader { get; private set; }

        public Task StartBackgroundTask(string objectName, TimeSpan leaseDuration, CancellationToken token)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentOutOfRangeException(nameof(objectName));
            }

            if (this.leaderObjectName != string.Empty)
            {
                throw new InvalidOperationException("Background task already started");
            }

            this.leaderObjectName = objectName;
            this.leaseDuration = leaseDuration;

            return Task.Run(() => this.BackgroundTaskLoop(token));
        }

        public async Task BackgroundTaskLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var record = await this.EnsureEndpointExists(token);

                    this.IsLeader = record.HolderIdentity == this.identity;
                    this.logger.LogInformation("LeaderElection/BackgroundTaskLoop: IsLeader={isLeader}", this.IsLeader);

                    var now = DateTimeOffset.UtcNow;
                    var expireDate = new DateTimeOffset((DateTime)(record.RenewTime!)) + TimeSpan.FromSeconds(record.LeaseDurationSeconds);
                    var timeToLive = expireDate - now;

                    if (this.IsLeader)
                    {
                        if (timeToLive > TimeToRefreshBeforeExpiry)
                        {
                            await Task.Delay(timeToLive - TimeToRefreshBeforeExpiry, token);
                        }

                        var newRecord = new LeaderElectionRecord()
                        {
                            HolderIdentity = this.identity,
                            LeaseDurationSeconds = (int)this.leaseDuration.TotalSeconds,
                            AcquireTime = record.AcquireTime,
                            RenewTime = DateTime.UtcNow,
                            LeaderTransitions = record.LeaderTransitions,
                        };

                        await this.UpdateLeaderRecord(newRecord, token);
                    }
                    else
                    {
                        if (timeToLive < TimeSpan.Zero)
                        {
                            var newRecord = new LeaderElectionRecord()
                            {
                                HolderIdentity = this.identity,
                                LeaseDurationSeconds = (int)this.leaseDuration.TotalSeconds,
                                AcquireTime = DateTime.UtcNow,
                                RenewTime = DateTime.UtcNow,
                                LeaderTransitions = record.LeaderTransitions + 1,
                            };

                            // it is expired--claim leadership and restart loop
                            await this.UpdateLeaderRecord(newRecord, token);
                            await Task.Delay(TimeToWaitAfterSettingLeader, token);
                            continue;
                        }
                        else
                        {
                            // Give the leader a little extra time to renew
                            await Task.Delay(timeToLive + TimeToWaitAfterSettingLeader, token);
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    this.logger.LogError(e, "BackgroundTaskLoop cancelled: {exception}", e.ToString());
                }
            }
        }

        private async Task<LeaderElectionRecord> EnsureEndpointExists(CancellationToken token)
        {
            var oldLeaderElectionRecord = await this.TryGetCurrentRecord(token);

            // if the current record is invalid, create one
            if (oldLeaderElectionRecord?.AcquireTime == null
                || oldLeaderElectionRecord?.RenewTime == null
                || oldLeaderElectionRecord?.HolderIdentity == null
                || oldLeaderElectionRecord.LeaseDurationSeconds > 60 * 10)
            {
                await this.UpdateLeaderRecord(
                    new LeaderElectionRecord()
                    {
                        HolderIdentity = this.identity,
                        LeaseDurationSeconds = (int)this.leaseDuration.TotalSeconds,
                        AcquireTime = DateTime.UtcNow,
                        RenewTime = DateTime.UtcNow,
                        LeaderTransitions = 0,
                    }, token);
            }

            var current = await this.TryGetCurrentRecord(token);
            if (current == null)
            {
                throw new Exception("LeaderElection EnsureEndpointExists: Not able to GetCurrentRecord after creating");
            }

            return current;
        }

        private async Task UpdateLeaderRecord(LeaderElectionRecord record, CancellationToken token)
        {
            this.logger.LogInformation("Updating leader record {leaderObjectName}", this.leaderObjectName);

            var annotations = new Dictionary<string, string>()
            {
                [LeaderAnnotationName] = JsonSerializer.Serialize(record),
            };

            await this.client.PutEndpointAnnotationsAsync(this.ns, this.leaderObjectName, annotations, token);
        }

        private async Task<LeaderElectionRecord?> TryGetCurrentRecord(CancellationToken token)
        {
            try
            {
                var annotations = await this.client.GetEndpointAnnotationsAsync(this.ns, this.leaderObjectName, token);
                if (annotations.TryGetValue(LeaderAnnotationName, out var jsonString))
                {
                    return JsonSerializer.Deserialize<LeaderElectionRecord>(jsonString);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Unhandled exception getting leader annotations from {namespace}/{leaderObjectName}: {exception}", this.ns, this.leaderObjectName, e.ToString());
                return null;
            }
        }

        internal class LeaderElectionRecord
        {
            public string? HolderIdentity { get; set; }

            public int LeaseDurationSeconds { get; set; }

            public DateTime? AcquireTime { get; set; }

            public DateTime? RenewTime { get; set; }

            public int LeaderTransitions { get; set; }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return this.Equals((LeaderElectionRecord)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.HolderIdentity != null ? this.HolderIdentity.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ this.AcquireTime.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.RenewTime.GetHashCode();
                    return hashCode;
                }
            }

            protected bool Equals(LeaderElectionRecord other)
            {
                return this.HolderIdentity == other?.HolderIdentity
                    && Nullable.Equals(this.AcquireTime, other?.AcquireTime)
                    && Nullable.Equals(this.RenewTime, other?.RenewTime);
            }
        }
    }
}

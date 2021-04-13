// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using k8s.LeaderElection;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    public class AutoCraneLeaderElector : IDisposable
    {
        private const double JitterFactor = 1.2;

        private readonly LeaderElectionConfig config;
        private readonly ILogger logger;
        private volatile LeaderElectionRecord? observedRecord;
        private DateTimeOffset observedTime = DateTimeOffset.MinValue;
        private string? reportedLeader;

        public AutoCraneLeaderElector(LeaderElectionConfig config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        /// <summary>
        /// OnStartedLeading is called when a LeaderElector client starts leading
        /// </summary>
        public event Action? OnStartedLeading;

        /// <summary>
        /// OnStoppedLeading is called when a LeaderElector client stops leading
        /// </summary>
        public event Action? OnStoppedLeading;

        /// <summary>
        /// OnNewLeader is called when the client observes a leader that is
        /// not the previously observed leader. This includes the first observed
        /// leader when the client starts.
        /// </summary>
        public event Action<string>? OnNewLeader;

        public bool IsLeader()
        {
            return this.observedRecord?.HolderIdentity != null && this.observedRecord?.HolderIdentity == this.config.Lock.Identity;
        }

        public string? GetLeader()
        {
            return this.observedRecord?.HolderIdentity;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await this.AcquireAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                this.logger.LogInformation($"Invoking OnStartedLeading");
                this.OnStartedLeading?.Invoke();

                // renew loop
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var acq = Task.Run(async () =>
                    {
                        try
                        {
                            while (!await this.TryAcquireOrRenew(cancellationToken).ConfigureAwait(false))
                            {
                                await Task.Delay(this.config.RetryPeriod, cancellationToken).ConfigureAwait(false);
                                this.MaybeReportTransition();
                            }
                        }
                        catch (Exception e)
                        {
                            // ignore
                            this.logger.LogError($"Exception in AutoCraneLeaderElector.acq: {e}");
                            return false;
                        }

                        return true;
                    });

                    if (await Task.WhenAny(acq, Task.Delay(this.config.RenewDeadline, cancellationToken))
                        .ConfigureAwait(false) == acq)
                    {
                        var succ = await acq.ConfigureAwait(false);

                        if (succ)
                        {
                            await Task.Delay(this.config.RetryPeriod, cancellationToken).ConfigureAwait(false);

                            // retry
                            continue;
                        }

                        // renew failed
                    }

                    // timeout
                    break;
                }
            }
            finally
            {
                this.logger.LogInformation($"Invoking OnStoppedLeading");
                this.OnStoppedLeading?.Invoke();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        private async Task<bool> TryAcquireOrRenew(CancellationToken cancellationToken)
        {
            var l = this.config.Lock;
            var leaderElectionRecord = new LeaderElectionRecord()
            {
                HolderIdentity = l.Identity,
                LeaseDurationSeconds = this.config.LeaseDuration.Seconds,
                AcquireTime = DateTime.UtcNow,
                RenewTime = DateTime.UtcNow,
                LeaderTransitions = 0,
            };

            // 1. obtain or create the ElectionRecord
            LeaderElectionRecord? oldLeaderElectionRecord = null;
            try
            {
                oldLeaderElectionRecord = await l.GetAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Microsoft.Rest.HttpOperationException e)
            {
                this.logger.LogError($"HttpOperationException: {e}");
                if (e.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    return false;
                }
            }

            if (oldLeaderElectionRecord?.AcquireTime == null ||
                oldLeaderElectionRecord?.RenewTime == null ||
                oldLeaderElectionRecord?.HolderIdentity == null)
            {
                var created = await l.CreateAsync(leaderElectionRecord, cancellationToken).ConfigureAwait(false);
                if (created)
                {
                    this.observedRecord = leaderElectionRecord;
                    this.observedTime = DateTimeOffset.Now;
                    this.logger.LogInformation($"Create leader election record");
                    return true;
                }

                this.logger.LogError($"Leader election records missing");
                return false;
            }

            // 2. Record obtained, check the Identity & Time
            if (!Equals(this.observedRecord, oldLeaderElectionRecord))
            {
                this.observedRecord = oldLeaderElectionRecord;
                this.observedTime = DateTimeOffset.Now;
            }

            if (!string.IsNullOrEmpty(oldLeaderElectionRecord.HolderIdentity)
                && this.observedTime + this.config.LeaseDuration > DateTimeOffset.Now
                && !this.IsLeader())
            {
                // lock is held by %v and has not yet expired", oldLeaderElectionRecord.HolderIdentity
                this.logger.LogInformation($"lock is held by {oldLeaderElectionRecord.HolderIdentity} and has not yet expired. Observed time: {this.observedTime}. Lease Duration: {this.config.LeaseDuration}");
                return false;
            }

            // 3. We're going to try to update. The leaderElectionRecord is set to it's default
            // here. Let's correct it before updating.
            if (this.IsLeader())
            {
                leaderElectionRecord.AcquireTime = oldLeaderElectionRecord.AcquireTime;
                leaderElectionRecord.LeaderTransitions = oldLeaderElectionRecord.LeaderTransitions;
            }
            else
            {
                leaderElectionRecord.LeaderTransitions = oldLeaderElectionRecord.LeaderTransitions + 1;
            }

            var updated = await l.UpdateAsync(leaderElectionRecord, cancellationToken).ConfigureAwait(false);
            if (!updated)
            {
                this.logger.LogError($"failed to update leader election record");
                return false;
            }

            this.observedRecord = leaderElectionRecord;
            this.observedTime = DateTimeOffset.Now;

            return true;
        }

        private async Task AcquireAsync(CancellationToken cancellationToken)
        {
            var delay = this.config.RetryPeriod;

            while (true)
            {
                try
                {
                    var acq = this.TryAcquireOrRenew(cancellationToken);

                    if (await Task.WhenAny(acq, Task.Delay(delay, cancellationToken))
                        .ConfigureAwait(false) == acq)
                    {
                        if (await acq.ConfigureAwait(false))
                        {
                            return;
                        }
                    }

                    delay *= JitterFactor;
                    this.logger.LogInformation($"Delay for {delay} ms");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    this.MaybeReportTransition();
                }
            }
        }

        private void MaybeReportTransition()
        {
            if (this.observedRecord == null)
            {
                return;
            }

            if (this.observedRecord.HolderIdentity == this.reportedLeader)
            {
                return;
            }

            this.reportedLeader = this.observedRecord.HolderIdentity;

            this.OnNewLeader?.Invoke(this.reportedLeader);
        }
    }
}

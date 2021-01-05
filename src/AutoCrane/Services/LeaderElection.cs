// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal class LeaderElection : ILeaderElection
    {
        public LeaderElection(KubernetesClient client, IAutoCraneConfig config, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<LeaderElection>();
            Task.Run(async () =>
            {
                try
                {
                    await client.SetupLeaderElectionAsync(
                        config.Namespaces.First(),
                        LeadershipElectionName,
                        CancellationToken.None,
                        () =>
                        {
                            logger.LogInformation("Became leader");
                            this.IsLeader = true;
                        },
                        () =>
                        {
                            logger.LogInformation("No longer leader");
                            this.IsLeader = false;
                        });
                }
                catch (Exception e)
                {
                    logger.LogError($"Unhandled exception in LeaderElection ctor: {e}");
                    throw;
                }
            });
        }

        public static string LeadershipElectionName { get; set; } = "autocrane";

        public bool IsLeader { get; private set; }
    }
}

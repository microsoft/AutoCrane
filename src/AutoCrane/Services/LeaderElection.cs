// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class LeaderElection : ILeaderElection
    {
        private readonly KubernetesClient client;

        public LeaderElection(KubernetesClient client)
        {
            this.client = client;
        }

        public Task<LeaderElectionResults> RequestAsync(string election)
        {
            return this.client.ElectLeaderAsync(election);
        }
    }
}

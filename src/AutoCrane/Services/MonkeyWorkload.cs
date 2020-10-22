// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class MonkeyWorkload : IMonkeyWorkload
    {
        private readonly Random random;
        private readonly IClock clock;
        private int failPercent;
        private DateTimeOffset failUntil;

        public MonkeyWorkload(IClock clock)
        {
            this.random = new Random();
            this.failPercent = 0;
            this.failUntil = DateTimeOffset.MinValue;
            this.clock = clock;
        }

        public bool ShouldFail()
        {
            var now = this.clock.Get();
            if (now > this.failUntil)
            {
                return false;
            }

            var randVal = this.random.Next(100);
            return this.failPercent > randVal;
        }

        public void SetFailPercentage(int pct, TimeSpan until)
        {
            this.failPercent = pct;
            this.failUntil = this.clock.Get() + until;
        }
    }
}

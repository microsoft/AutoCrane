using System;
using AutoCrane.Interfaces;

namespace AutoCrane.Tests
{
    internal class ManualClock : IClock
    {
        public DateTimeOffset Time { get; set; }

        public DateTimeOffset Get()
        {
            return this.Time;
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class DefaultClock : IClock
    {
        public DateTimeOffset Get()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}

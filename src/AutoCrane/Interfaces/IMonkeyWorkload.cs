// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Interfaces
{
    public interface IMonkeyWorkload
    {
        bool ShouldFail();

        public void SetFailPercentage(int pct, TimeSpan until);
    }
}

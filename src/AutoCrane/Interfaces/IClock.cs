// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Interfaces
{
    public interface IClock
    {
        DateTimeOffset Get();
    }
}

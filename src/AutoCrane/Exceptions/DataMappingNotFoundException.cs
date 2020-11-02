// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Exceptions
{
    public sealed class DataMappingNotFoundException : Exception
    {
        public DataMappingNotFoundException(string msg)
            : base(msg)
        {
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Exceptions
{
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException(string msg)
            : base(msg)
        {
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Interfaces
{
    public interface IProcessResult
    {
        string Executable { get; }

        int ExitCode { get; }

        IList<string> OutputText { get; }

        IList<string> ErrorText { get; }

        void ThrowIfFailed();
    }
}
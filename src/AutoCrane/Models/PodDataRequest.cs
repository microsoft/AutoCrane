// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public class PodDataRequest
    {
        public PodDataRequest(string name, string source)
        {
            this.Name = name;
            this.Source = source;
        }

        public string Name { get; }

        public string Source { get; }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class DropManifestReaderFactory : IDropManifestReaderFactory
    {
        public IDropManifestReader FromStream(Stream s)
        {
            return new DropManifestReader(s);
        }

        private class DropManifestReader : IDropManifestReader
        {
            private readonly StreamReader reader;

            public DropManifestReader(Stream s)
            {
                this.reader = new StreamReader(s);
            }

            public void Dispose()
            {
                this.reader.Dispose();
            }

            public IEnumerable<DropManifestEntry> Read()
            {
                string? line;

                while ((line = this.reader.ReadLine()) != null)
                {
                    var splits = line.Split(' ', 3);
                    if (line.StartsWith("#") || splits.Length != 3)
                    {
                        continue;
                    }

                    yield return new DropManifestEntry(splits[0], splits[1], splits[2]);
                }
            }
        }
    }
}

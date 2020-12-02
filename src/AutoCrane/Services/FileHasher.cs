// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    public sealed class FileHasher : IFileHasher
    {
        public async Task<string> CalculateAsync(string filename, bool cacheOnDisk = false)
        {
            if (cacheOnDisk == false)
            {
                return await CalculateAsync(filename);
            }
            else
            {
                var cachedFilename = filename + ".hash";
                if (File.Exists(cachedFilename))
                {
                    return await File.ReadAllTextAsync(cachedFilename);
                }
                else
                {
                    var hash = await CalculateAsync(filename);
                    await File.WriteAllTextAsync(cachedFilename, hash);
                    return hash;
                }
            }
        }

        private static async Task<string> CalculateAsync(string filename)
        {
            using var fs = File.OpenRead(filename);
            using var sha = SHA256.Create();
            var hashBinary = await sha.ComputeHashAsync(fs);
            var hash = Convert.ToHexString(hashBinary);
            return hash;
        }
    }
}

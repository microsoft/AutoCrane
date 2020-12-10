// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using AutoCrane.Interfaces;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequestDetails
    {
        public DataDownloadRequestDetails(string path, string hash)
        {
            this.Path = path;
            this.Hash = hash;
        }

        /// <summary>
        /// The data repository host name.
        /// </summary>
        public string? Path { get; }

        /// <summary>
        /// A hash of the contents of the archive.
        /// </summary>
        public string? Hash { get; }

        /// <summary>
        /// Number of seconds since the unix epoch.
        /// </summary>
        public long? UnixTimestampSeconds { get; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static DataDownloadRequestDetails? FromBase64Json(string str)
        {
            try
            {
                var utf8json = Convert.FromBase64String(str);
                var details = JsonSerializer.Deserialize<DataDownloadRequestDetails>(utf8json);
                return details;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is DataDownloadRequestDetails details &&
                   this.Path == details.Path &&
                   this.Hash == details.Hash /*&&
                   this.UnixTimestampSeconds == details.UnixTimestampSeconds*/;
        }

        public string ToBase64String()
        {
            return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this));
        }

        public override string? ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using AutoCrane.Interfaces;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequestDetails
    {
        /// <summary>
        /// The data repository host name.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// A hash of the contents of the archive.
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// Number of seconds since the unix epoch.
        /// </summary>
        public long UnixTimestampSeconds { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static DataDownloadRequestDetails? FromBase64Json(string str)
        {
            var utf8json = Convert.FromBase64String(str);
            var details = JsonSerializer.Deserialize<DataDownloadRequestDetails>(utf8json);
            return details;
        }

        public string ToBase64String()
        {
            return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this));
        }

        public void UpdateTimestamp(IClock clock)
        {
            this.UnixTimestampSeconds = clock.Get().ToUnixTimeSeconds();
        }
    }
}

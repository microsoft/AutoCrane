// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AutoCrane
{
    public static class WebHosting
    {
        public static void RunWebService<TStartup>(string[] args)
                where TStartup : class
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel((ctx, options) =>
                {
                    var listenPortString = Environment.GetEnvironmentVariable("LISTEN_PORT");
                    var listenPort = string.IsNullOrEmpty(listenPortString) ? 8080 : int.Parse(listenPortString);
                    options.Listen(IPAddress.Any, listenPort);
                    Console.WriteLine($"Listening on {listenPort}");
                });

            var host = builder.UseStartup<TStartup>().Build();
            host.Run();
        }
    }
}

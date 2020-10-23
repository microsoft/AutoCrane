// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    public class WatchdogHealthz
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ASP.NET")]
        public void ConfigureServices(IServiceCollection services)
        {
            DependencyInjection.Setup(new ConfigurationBuilder().AddEnvironmentVariables().Build(), services);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ASP.NET")]
        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<LogRequestMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/ping", (ctx) =>
                {
                    return ctx.Response.WriteAsync("ok");
                });

                endpoints.MapGet("/healthz", async (ctx) =>
                {
                    var healthMonitor = ctx.RequestServices.GetRequiredService<IConsecutiveHealthMonitor>();
                    var options = ctx.RequestServices.GetRequiredService<IOptions<PodIdentifierOptions>>();
                    var ns = options.Value.Namespace ?? string.Empty;
                    var name = options.Value.Name ?? string.Empty;
                    if (ns == string.Empty)
                    {
                        throw new ArgumentNullException(nameof(options.Value.Namespace));
                    }

                    if (name == string.Empty)
                    {
                        throw new ArgumentNullException(nameof(options.Value.Name));
                    }

                    var podid = new PodIdentifier(ns, name);
                    await healthMonitor.Probe(podid);
                    if (healthMonitor.IsHealthy(podid))
                    {
                        ctx.Response.StatusCode = 200;
                    }
                    else
                    {
                        ctx.Response.StatusCode = 500;
                    }
                });
            });
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using AutoCrane.Interfaces;
using AutoCrane.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public class DataDeployer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ASP.NET")]
        public void ConfigureServices(IServiceCollection services)
        {
            DependencyInjection.Setup(new ConfigurationBuilder().AddEnvironmentVariables().Build(), services);
            services.AddHostedService<DataDeploymentBackgroundSync>();
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

                endpoints.MapGet("/watchdog", (ctx) =>
                {
                    var hb = ctx.RequestServices.GetRequiredService<IServiceHeartbeat>();
                    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DataDeployer));
                    var lastBeat = hb.GetLastBeat(nameof(DataDeploymentBackgroundSync));
                    if (!lastBeat.HasValue || lastBeat.Value > DataDeploymentBackgroundSync.HeartbeatTimeout)
                    {
                        ctx.Response.StatusCode = 500;
                        var msg = $"Error, last heartbeat (zero if none): {lastBeat.GetValueOrDefault().TotalSeconds} sec ago.";
                        logger.LogInformation(msg);
                        return ctx.Response.WriteAsync(msg);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 200;
                        var msg = $"Ok, last heartbeat: {lastBeat.GetValueOrDefault().TotalSeconds} sec ago.";
                        logger.LogInformation(msg);
                        return ctx.Response.WriteAsync("ok");
                    }
                });
            });
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    public class DataRepository
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ASP.NET")]
        public void ConfigureServices(IServiceCollection services)
        {
            DependencyInjection.Setup(new ConfigurationBuilder().AddEnvironmentVariables().Build(), services);
            services.AddHostedService<DataRepositoryCrawler>();
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
                    var opts = ctx.RequestServices.GetRequiredService<IOptions<DataRepoOptions>>();
                    var heartbeat = ctx.RequestServices.GetRequiredService<IServiceHeartbeat>();
                    var lastDataRepoCrawl = heartbeat.GetLastBeat(nameof(DataRepositoryCrawler));
                    if (!lastDataRepoCrawl.HasValue || lastDataRepoCrawl.Value > DataRepositoryCrawler.HeartbeatTimeout)
                    {
                        ctx.Response.StatusCode = 500;
                        return ctx.Response.WriteAsync("crawler not running");
                    }

                    if (string.IsNullOrEmpty(opts.Value.Path))
                    {
                        ctx.Response.StatusCode = 500;
                        return ctx.Response.WriteAsync("path not set");
                    }

                    if (!Directory.Exists(opts.Value.Path))
                    {
                        ctx.Response.StatusCode = 500;
                        return ctx.Response.WriteAsync("path does not exist");
                    }

                    return ctx.Response.WriteAsync("ok");
                });

                endpoints.MapGet("/{file}", (ctx) =>
                {
                    var opts = ctx.RequestServices.GetRequiredService<IOptions<DataRepoOptions>>();
                    var file = ctx.Request.RouteValues["file"]?.ToString();
                    var fileRoot = opts.Value.Path;
                    if (file is null || fileRoot is null)
                    {
                        ctx.Response.StatusCode = 400;
                        return Task.CompletedTask;
                    }

                    var filePath = Path.Combine(fileRoot, file);
                    if (File.Exists(filePath))
                    {
                        using var fs = new FileStream(filePath, FileMode.Open);
                        return fs.CopyToAsync(ctx.Response.Body);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                        return Task.CompletedTask;
                    }
                });
            });
        }
    }
}

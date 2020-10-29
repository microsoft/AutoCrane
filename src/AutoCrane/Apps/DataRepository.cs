// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using AutoCrane.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                    return ctx.Response.WriteAsync("ok");
                });

                var fileRoot = Environment.GetEnvironmentVariable("FILE_ROOT");
                if (string.IsNullOrEmpty(fileRoot))
                {
                    throw new ArgumentNullException("FILE_ROOT not set");
                }

                endpoints.MapGet("/{file}", (ctx) =>
                {
                    var file = ctx.Request.RouteValues["file"]?.ToString();
                    if (file is null)
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

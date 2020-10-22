// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoCrane.Apps
{
    public class TestWorkload
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

                endpoints.MapGet("/watchdog", (ctx) =>
                {
                    var mw = ctx.RequestServices.GetRequiredService<IMonkeyWorkload>();
                    if (mw.ShouldFail())
                    {
                        ctx.Response.StatusCode = 500;
                    }
                    else
                    {
                        ctx.Response.StatusCode = 200;
                    }

                    return Task.CompletedTask;
                });

                endpoints.MapPost("/fail", (ctx) =>
                {
                    var mw = ctx.RequestServices.GetRequiredService<IMonkeyWorkload>();
                    mw.SetFailPercentage(100, TimeSpan.FromMinutes(10));
                    return Task.CompletedTask;
                });
            });
        }
    }
}

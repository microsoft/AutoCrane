// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrane.Exceptions;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoCrane.Apps
{
    public class WatchdogListener
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

                endpoints.Map("/watchdogs/{ns}/{pod}", async (ctx) =>
                {
                    if (ctx.Request.Method == HttpMethods.Get)
                    {
                        await HandlePodGet(ctx);
                    }
                    else if (ctx.Request.Method == HttpMethods.Put)
                    {
                        await HandlePodPut(ctx);
                    }
                    else
                    {
                        // verb not supported
                        ctx.Response.StatusCode = 405;
                    }
                });
            });
        }

        internal static async Task HandlePodGet(HttpContext ctx)
        {
            var namespaceName = ctx.GetRouteValue("ns")?.ToString() ?? string.Empty;
            var podName = ctx.GetRouteValue("pod")?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(podName))
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            var getter = ctx.RequestServices.GetRequiredService<IWatchdogStatusGetter>();
            var piFactory = ctx.RequestServices.GetRequiredService<IPodIdentifierFactory>();
            try
            {
                var status = await getter.GetStatusAsync(piFactory.FromString(namespaceName, podName));
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(status));
            }
            catch (PodNotFoundException)
            {
                ctx.Response.StatusCode = 404;
            }
            catch (ForbiddenException)
            {
                ctx.Response.StatusCode = 403;
            }
        }

        internal static async Task HandlePodPut(HttpContext ctx)
        {
            var putter = ctx.RequestServices.GetRequiredService<IWatchdogStatusPutter>();
            var piFactory = ctx.RequestServices.GetRequiredService<IPodIdentifierFactory>();
            try
            {
                var namespaceName = ctx.GetRouteValue("ns")?.ToString() ?? string.Empty;
                var podName = ctx.GetRouteValue("pod")?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(podName))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync("empty");
                    return;
                }

                var status = await JsonSerializer.DeserializeAsync<WatchdogStatus>(ctx.Request.Body);
                if (status == null || string.IsNullOrEmpty(status?.Name))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("invalid name");
                    return;
                }

                if (status.Message is null)
                {
                    status.Message = string.Empty;
                }

                if (!WatchdogStatus.ValidLevels.Contains(status?.Level ?? string.Empty))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("invalid level");
                    return;
                }

                ctx.Response.StatusCode = 200;
                await putter.PutStatusAsync(piFactory.FromString(namespaceName, podName), status!);
            }
            catch (PodNotFoundException)
            {
                ctx.Response.StatusCode = 404;
            }
            catch (ForbiddenException)
            {
                ctx.Response.StatusCode = 403;
            }
            catch (JsonException e)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.WriteAsync(e.Message);
            }
        }
    }
}

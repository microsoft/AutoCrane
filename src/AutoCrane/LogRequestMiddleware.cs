// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace AutoCrane
{
    public sealed class LogRequestMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;

        public LogRequestMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.logger = loggerFactory.CreateLogger(nameof(LogRequestMiddleware));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            Exception? exceptionToReport = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await this.next(httpContext);
            }
            catch (Exception exception)
            {
                exceptionToReport = exception;
            }
            finally
            {
                stopwatch.Stop();
            }

            this.logger.LogInformation($"{httpContext.Response.StatusCode} {stopwatch.ElapsedMilliseconds}ms '{httpContext.Response.ContentType ?? "null"}' {httpContext.Response.ContentLength ?? -1} {httpContext.Request.GetDisplayUrl()}");

            if (exceptionToReport != null)
            {
                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = 500;
                }

                this.logger.LogError($"{httpContext.Response.StatusCode} {stopwatch.ElapsedMilliseconds}ms '{httpContext.Response.ContentType ?? "null"}' {httpContext.Response.ContentLength ?? -1} {httpContext.Request.GetDisplayUrl()} {exceptionToReport.ToString().Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal)}");
                throw exceptionToReport;
            }
        }
    }
}

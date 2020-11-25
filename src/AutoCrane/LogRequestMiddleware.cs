// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace AutoCrane
{
    public sealed class LogRequestMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly Histogram latencyMetric;
        private readonly Counter requestMetric;

        public LogRequestMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.logger = loggerFactory.CreateLogger(nameof(LogRequestMiddleware));
            this.latencyMetric = Metrics.CreateHistogram("autocrane_request_latency_milliseconds", "Latency of HTTP Requests", new HistogramConfiguration() { Buckets = Histogram.ExponentialBuckets(start: 1, 2, count: 10), LabelNames = new string[] { "method", "path" } });
            this.requestMetric = Metrics.CreateCounter("autocrane_request_count", "Count of HTTP requests", new CounterConfiguration() { LabelNames = new string[] { "method", "path", "code" } });
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

            this.latencyMetric.WithLabels(httpContext.Request.Method, httpContext.Request.Path).Observe(stopwatch.ElapsedMilliseconds);

            if (exceptionToReport != null)
            {
                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = 500;
                }

                this.requestMetric.WithLabels(httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode.ToString()).Inc();
                this.logger.LogError($"{httpContext.Response.StatusCode} {stopwatch.ElapsedMilliseconds}ms '{httpContext.Response.ContentType ?? "null"}' {httpContext.Response.ContentLength ?? -1} {httpContext.Request.GetDisplayUrl()} {exceptionToReport.ToString().Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal)}");
                throw exceptionToReport;
            }
            else
            {
                this.requestMetric.WithLabels(httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode.ToString()).Inc();
                this.logger.LogInformation($"{httpContext.Response.StatusCode} {stopwatch.ElapsedMilliseconds}ms '{httpContext.Response.ContentType ?? "null"}' {httpContext.Response.ContentLength ?? -1} {httpContext.Request.GetDisplayUrl()}");
            }
        }
    }
}

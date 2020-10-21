using AutoCrane.Apps;
using AutoCrane.Interfaces;
using AutoCrane.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Tests
{
    [TestClass]
    public class WatchdogListenerTests
    {
        [DataRow("", "", "")]
        [DataRow("", "", "")]
        [DataRow("a", "", "")]
        [DataRow("", "b", "")]
        [DataTestMethod]
        public async Task TestGetBadRequest(string ns, string pod, string body)
        {
            var ctx = GetHttpContext(ns, pod, body, (sc) => { sc.AddSingleton(new Mock<IWatchdogStatusGetter>().Object); });

            await WatchdogListener.HandlePodGet(ctx);

            Assert.AreEqual(400, ctx.Response.StatusCode);
        }

        [DataRow("", "", "")]
        [DataRow("", "", "")]
        [DataRow("a", "", "")]
        [DataRow("", "b", "")]
        [DataRow("a", "b", "")]
        [DataRow("a", "b", "{}")]
        [DataRow("a", "b", "{\"Name\": \"a\", \"Level\": \"a\", \"Message\": \"hi\"}")]
        [DataRow("a", "b", "{\"Name\": \"\", \"Level\": \"a\", \"Message\": \"hi\"}")]
        [DataRow("a", "b", "{\"Name\": \"a\", \"Level\": \"\", \"Message\": \"hi\"}")]
        [DataRow("", "b", "{\"Name\": \"a\", \"Level\": \"Info\", \"Message\": \"hi\"}")]
        [DataRow("a", "", "{\"Name\": \"a\", \"Level\": \"Info\", \"Message\": \"hi\"}")]
        [DataTestMethod]
        public async Task TestPutBadRequest(string ns, string pod, string body)
        {
            var ctx = GetHttpContext(ns, pod, body, (sc) => { sc.AddSingleton(new Mock<IWatchdogStatusPutter>().Object); });

            await WatchdogListener.HandlePodPut(ctx);

            Assert.AreEqual(400, ctx.Response.StatusCode);
        }


        [DataRow("a", "b", "{\"Name\": \"c\", \"Level\": \"Info\", \"Message\": \"hi\"}")]
        [DataTestMethod]
        public async Task TestPutOk(string ns, string pod, string body)
        {
            var ctx = GetHttpContext(ns, pod, body, (sc) => { sc.AddSingleton(new Mock<IWatchdogStatusPutter>().Object); });

            await WatchdogListener.HandlePodPut(ctx);

            Assert.AreEqual(200, ctx.Response.StatusCode);
        }

        internal static HttpContext GetHttpContext(string? ns, string? pod, string requestBody, Action<ServiceCollection> provider)
        {
            var ctx = new Mock<HttpContext>();
            var resp = new Mock<HttpResponse>();
            var req = new Mock<HttpRequest>();
            var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            var rvd = RouteValueDictionary.FromArray(new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("ns", ns),
                new KeyValuePair<string, object?>("pod", pod),
            });

            var fc = new FeatureCollection();
            var rvf = new RouteValuesFeature() { RouteValues = rvd };
            fc.Set<IRouteValuesFeature>(rvf);

            req.Setup(s => s.Body).Returns(requestBodyStream);
            resp.SetupProperty(s => s.StatusCode);
            resp.Setup(s => s.BodyWriter).Returns(new FakePipeWriter());
            ctx.Setup(s => s.Request).Returns(req.Object);
            ctx.Setup(s => s.Response).Returns(resp.Object);
            ctx.Setup(s => s.Features).Returns(fc);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IPodIdentifierFactory, PodIdentifierFactory>();
            provider(serviceCollection);
            var sp = serviceCollection.BuildServiceProvider();
            ctx.Setup(s => s.RequestServices).Returns(sp);
            return ctx.Object;
        }
    }
}

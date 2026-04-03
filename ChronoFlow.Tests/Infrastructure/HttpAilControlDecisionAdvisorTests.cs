using System.Net;
using ChronoFlow.Infrastructure.ControlTriggers;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChronoFlow.Tests.Infrastructure;

public sealed class HttpAilControlDecisionAdvisorTests
{
    [Fact]
    public async Task Returns_null_when_AIL_returns_non_success_without_throwing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var opts = Options.Create(new ControlTriggersAdvisoryOptions
        {
            Enabled = true,
            BaseUrl = "http://localhost/"
        });
        var sut = new HttpAilControlDecisionAdvisor(http, opts, NullLogger<HttpAilControlDecisionAdvisor>.Instance);
        var cmd = new ReceiveControlTriggerCommand(
            "AlertCreated",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "R",
            false);

        var result = await sut.GetAdvisoryAsync(cmd, CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(_respond(request));
        }
    }
}

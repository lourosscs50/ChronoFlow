using System.Net;
using System.Text.Json;
using ChronoFlow.Infrastructure.ControlTriggers;
using ChronoFlow.Modules.ControlTriggers.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChronoFlow.Tests.Infrastructure;

public sealed class HttpAilExecutionClientTests
{
    [Fact]
    public async Task Returns_succeeded_with_mapped_outcome_and_linked_ail_id_when_AIL_returns_200()
    {
        const string json = """
            {"selectedStrategyKey":"default_safe","confidence":"High","reasonSummary":"ok","usedMemory":false,"memoryItemCount":0,"executionId":"ail-dec-42"}
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var opts = Options.Create(new ControlTriggersAdvisoryOptions
        {
            Enabled = true,
            BaseUrl = "http://localhost/"
        });
        var sut = new HttpAilExecutionClient(http, opts, NullLogger<HttpAilExecutionClient>.Instance);
        var orchId = Guid.Parse("f1000000-0000-4000-8000-000000000099");
        var request = new AdvisoryExecutionRequest(
            "AlertCreated",
            Guid.Parse("a1000000-0000-4000-8000-000000000011"),
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            "Open",
            "AlertCreated",
            "Rule",
            false,
            "corr-upstream-1",
            orchId);

        var result = await sut.RequestAdvisoryAsync(request, CancellationToken.None);

        var succeeded = Assert.IsType<AdvisoryExecutionResult.Succeeded>(result);
        Assert.Equal("default_safe", succeeded.Outcome.SelectedStrategyKey);
        Assert.Equal("High", succeeded.Outcome.Confidence);
        Assert.Equal("ail-dec-42", succeeded.Outcome.LinkedAilExecutionId);
    }

    [Fact]
    public async Task Returns_unavailable_when_AIL_returns_5xx()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var opts = Options.Create(new ControlTriggersAdvisoryOptions
        {
            Enabled = true,
            BaseUrl = "http://localhost/"
        });
        var sut = new HttpAilExecutionClient(http, opts, NullLogger<HttpAilExecutionClient>.Instance);
        var request = MinimalRequest();

        var result = await sut.RequestAdvisoryAsync(request, CancellationToken.None);

        var unavailable = Assert.IsType<AdvisoryExecutionResult.Unavailable>(result);
        Assert.Equal("ail_http_5xx", unavailable.ReasonCode);
    }

    [Fact]
    public async Task Posts_metadata_with_correlation_and_orchestration_execution_instance_id()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            const string json = """
                {"selectedStrategyKey":"default_safe","confidence":"Low","reasonSummary":"","usedMemory":false,"memoryItemCount":0}
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var opts = Options.Create(new ControlTriggersAdvisoryOptions
        {
            Enabled = true,
            BaseUrl = "http://localhost/",
            DefaultTenantId = Guid.Parse("d1000000-0000-4000-8000-000000000001"),
            DecisionType = "control_trigger_routing"
        });
        var sut = new HttpAilExecutionClient(http, opts, NullLogger<HttpAilExecutionClient>.Instance);
        var orchId = Guid.Parse("e2000000-0000-4000-8000-000000000088");
        var request = new AdvisoryExecutionRequest(
            "AlertCreated",
            Guid.Parse("a1000000-0000-4000-8000-000000000011"),
            Guid.Parse("b2000000-0000-4000-8000-000000000022"),
            Guid.Parse("c3000000-0000-4000-8000-000000000033"),
            "Open",
            "AlertCreated",
            null,
            false,
            "ext-corr-xyz",
            orchId);

        _ = await sut.RequestAdvisoryAsync(request, CancellationToken.None);

        Assert.NotNull(captured);
        var body = await captured.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var metadata = root.GetProperty("metadata");
        Assert.Equal("ext-corr-xyz", metadata.GetProperty("correlationId").GetString());
        Assert.Equal(orchId.ToString("D"), metadata.GetProperty("orchestrationExecutionInstanceId").GetString());
    }

    private static AdvisoryExecutionRequest MinimalRequest() =>
        new(
            "AlertCreated",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Open",
            "AlertCreated",
            null,
            false,
            null,
            Guid.NewGuid());

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

public sealed class HttpAilControlDecisionAdvisorTests
{
    [Fact]
    public async Task When_advisory_disabled_returns_skipped_and_does_not_invoke_ail_client()
    {
        var throwing = new ThrowingAilExecutionClient();
        var sut = new HttpAilControlDecisionAdvisor(
            throwing,
            Options.Create(new ControlTriggersAdvisoryOptions
            {
                Enabled = false,
                BaseUrl = "http://localhost/"
            }),
            NullLogger<HttpAilControlDecisionAdvisor>.Instance);
        var cmd = MinimalCommand();

        var result = await sut.GetAdvisoryAsync(cmd, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<AdvisoryExecutionResult.SkippedNotRequested>(result);
    }

    [Fact]
    public async Task When_base_url_missing_returns_unavailable_without_invoking_ail_client()
    {
        var throwing = new ThrowingAilExecutionClient();
        var sut = new HttpAilControlDecisionAdvisor(
            throwing,
            Options.Create(new ControlTriggersAdvisoryOptions
            {
                Enabled = true,
                BaseUrl = ""
            }),
            NullLogger<HttpAilControlDecisionAdvisor>.Instance);
        var cmd = MinimalCommand();

        var result = await sut.GetAdvisoryAsync(cmd, Guid.NewGuid(), CancellationToken.None);

        var u = Assert.IsType<AdvisoryExecutionResult.Unavailable>(result);
        Assert.Equal("ail_base_url_missing", u.ReasonCode);
    }

    private static ReceiveControlTriggerCommand MinimalCommand() =>
        new(
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
            null,
            false);

    private sealed class ThrowingAilExecutionClient : IAilExecutionClient
    {
        public Task<AdvisoryExecutionResult> RequestAdvisoryAsync(
            AdvisoryExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = cancellationToken;
            throw new InvalidOperationException("AIL client should not be invoked.");
        }
    }
}

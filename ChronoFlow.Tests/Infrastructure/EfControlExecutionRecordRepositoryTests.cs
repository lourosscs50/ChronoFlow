using ChronoFlow.Modules.ControlTriggers.Application;
using ChronoFlow.Modules.ControlTriggers.Domain;
using ChronoFlow.Modules.Events.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChronoFlow.Tests.Infrastructure;

public sealed class EfControlExecutionRecordRepositoryTests
{
    [Fact]
    public async Task Add_get_list_round_trip_with_ef_configuration()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase($"exec-records-{Guid.NewGuid():N}")
            .Options;

        await using (var ctx = new EventsDbContext(options))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var executionInstanceId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var alertId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var record = new ControlExecutionRecord
        {
            Id = id,
            ExecutionInstanceId = executionInstanceId,
            TriggerType = "AlertCreated",
            LifecycleEventType = "AlertCreated",
            AlertId = alertId,
            RuleId = Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            SignalId = Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            CorrelationId = "corr-ef-roundtrip",
            WorkflowKey = "alert-created-default",
            WasExecuted = true,
            WasSuppressed = false,
            SuppressionReason = null,
            ExecutedStepCount = 3,
            ReceivedAtUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero),
            ExecutedAtUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 1, TimeSpan.Zero),
            CurrentStatus = "Open",
            RuleName = "R",
            HasBeenReopened = false,
            AdvisoryWasUsed = false,
            AdvisoryStrategyKey = null,
            AdvisoryConfidence = null,
            AdvisoryReasonSummary = null,
            LinkedAilExecutionId = null,
            InboundDecisionSummary = null,
            InboundDecisionReferenceId = null,
            InboundDecisionConfidence = null,
            InboundDecisionReasonCode = null,
            InboundLinkedExternalExecutionId = null,
            PendingOperatorReview = false,
            OrchestrationPolicyOutcome = OrchestrationPolicyOutcomes.Proceed
        };

        await using (var ctx = new EventsDbContext(options))
        {
            var sut = new EfControlExecutionRecordRepository(ctx);
            await sut.AddAsync(record);

            var loaded = await sut.GetByIdAsync(id);
            Assert.NotNull(loaded);
            Assert.Equal("alert-created-default", loaded!.WorkflowKey);
            Assert.True(loaded.WasExecuted);
            Assert.Equal(3, loaded.ExecutedStepCount);
            Assert.Equal(executionInstanceId, loaded.ExecutionInstanceId);
            Assert.Equal("corr-ef-roundtrip", loaded.CorrelationId);

            var list = await sut.ListAsync(
                new ControlExecutionRecordQuery(alertId, null, null, null, null, 0, 10));
            Assert.Single(list);
            Assert.Equal(id, list[0].Id);
        }
    }
}

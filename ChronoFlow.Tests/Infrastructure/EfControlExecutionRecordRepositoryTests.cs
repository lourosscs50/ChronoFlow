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
        var alertId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var record = new ControlExecutionRecord
        {
            Id = id,
            TriggerType = "AlertCreated",
            LifecycleEventType = "AlertCreated",
            AlertId = alertId,
            RuleId = Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            SignalId = Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            WorkflowKey = "alert-created-default",
            WasExecuted = true,
            WasSuppressed = false,
            SuppressionReason = null,
            ExecutedStepCount = 3,
            ReceivedAtUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero),
            ExecutedAtUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 1, TimeSpan.Zero),
            CurrentStatus = "Open",
            RuleName = "R",
            AdvisoryWasUsed = false,
            AdvisoryStrategyKey = null,
            AdvisoryConfidence = null,
            AdvisoryReasonSummary = null
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

            var list = await sut.ListAsync(
                new ControlExecutionRecordQuery(alertId, null, null, null, null, 0, 10));
            Assert.Single(list);
            Assert.Equal(id, list[0].Id);
        }
    }
}

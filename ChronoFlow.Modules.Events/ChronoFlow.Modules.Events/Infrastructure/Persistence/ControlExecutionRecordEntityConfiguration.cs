using ChronoFlow.Modules.ControlTriggers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence;

public sealed class ControlExecutionRecordEntityConfiguration : IEntityTypeConfiguration<ControlExecutionRecord>
{
    public void Configure(EntityTypeBuilder<ControlExecutionRecord> builder)
    {
        builder.ToTable("control_execution_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TriggerType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LifecycleEventType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.WorkflowKey).HasMaxLength(200);
        builder.Property(x => x.SuppressionReason).HasMaxLength(2000);
        builder.Property(x => x.CurrentStatus).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AcknowledgedByUserId).HasMaxLength(200);
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(200);
        builder.Property(x => x.ReopenedByUserId).HasMaxLength(200);
        builder.Property(x => x.RuleName).HasMaxLength(500);

        builder.Property(x => x.AdvisoryStrategyKey).HasMaxLength(100);
        builder.Property(x => x.AdvisoryConfidence).HasMaxLength(50);
        builder.Property(x => x.AdvisoryReasonSummary).HasMaxLength(500);
        builder.Property(x => x.LinkedAilExecutionId).HasMaxLength(200);
        builder.Property(x => x.InboundDecisionSummary).HasMaxLength(500);
        builder.Property(x => x.InboundDecisionReferenceId).HasMaxLength(200);
        builder.Property(x => x.InboundDecisionConfidence).HasMaxLength(50);
        builder.Property(x => x.InboundDecisionReasonCode).HasMaxLength(100);
        builder.Property(x => x.InboundLinkedExternalExecutionId).HasMaxLength(200);

        builder.Property(x => x.OrchestrationPolicyOutcome).HasMaxLength(50);
        builder.Property(x => x.PendingOperatorReview).IsRequired();

        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.HasIndex(x => x.AlertId);
        builder.HasIndex(x => x.LifecycleEventType);
        builder.HasIndex(x => x.ReceivedAtUtc);
        builder.HasIndex(x => x.WasExecuted);
        builder.HasIndex(x => x.WasSuppressed);
        builder.HasIndex(x => x.WorkflowKey);
        builder.HasIndex(x => x.ExecutionInstanceId);
    }
}

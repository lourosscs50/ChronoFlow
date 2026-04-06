using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class OrchestrationPolicyFieldsOnControlExecutionRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OrchestrationPolicyOutcome",
            table: "control_execution_records",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PendingOperatorReview",
            table: "control_execution_records",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OrchestrationPolicyOutcome",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "PendingOperatorReview",
            table: "control_execution_records");
    }
}

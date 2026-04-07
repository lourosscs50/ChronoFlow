using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InboundDecisionAndLinkedAilOnControlExecutionRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "InboundDecisionConfidence",
            table: "control_execution_records",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InboundDecisionReasonCode",
            table: "control_execution_records",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InboundDecisionReferenceId",
            table: "control_execution_records",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InboundDecisionSummary",
            table: "control_execution_records",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InboundLinkedExternalExecutionId",
            table: "control_execution_records",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LinkedAilExecutionId",
            table: "control_execution_records",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InboundDecisionConfidence",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "InboundDecisionReasonCode",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "InboundDecisionReferenceId",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "InboundDecisionSummary",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "InboundLinkedExternalExecutionId",
            table: "control_execution_records");

        migrationBuilder.DropColumn(
            name: "LinkedAilExecutionId",
            table: "control_execution_records");
    }
}

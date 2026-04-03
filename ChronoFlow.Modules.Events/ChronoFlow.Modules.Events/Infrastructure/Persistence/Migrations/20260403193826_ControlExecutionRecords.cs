using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ControlExecutionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "control_execution_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LifecycleEventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WasExecuted = table.Column<bool>(type: "boolean", nullable: false),
                    WasSuppressed = table.Column<bool>(type: "boolean", nullable: false),
                    SuppressionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExecutedStepCount = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AcknowledgedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReopenedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasBeenReopened = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_execution_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_AlertId",
                table: "control_execution_records",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_LifecycleEventType",
                table: "control_execution_records",
                column: "LifecycleEventType");

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_ReceivedAtUtc",
                table: "control_execution_records",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_WasExecuted",
                table: "control_execution_records",
                column: "WasExecuted");

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_WasSuppressed",
                table: "control_execution_records",
                column: "WasSuppressed");

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_WorkflowKey",
                table: "control_execution_records",
                column: "WorkflowKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_execution_records");
        }
    }
}

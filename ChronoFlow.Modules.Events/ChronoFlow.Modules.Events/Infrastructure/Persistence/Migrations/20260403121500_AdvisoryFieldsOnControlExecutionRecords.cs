using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdvisoryFieldsOnControlExecutionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdvisoryWasUsed",
                table: "control_execution_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdvisoryStrategyKey",
                table: "control_execution_records",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvisoryConfidence",
                table: "control_execution_records",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvisoryReasonSummary",
                table: "control_execution_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvisoryWasUsed",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "AdvisoryStrategyKey",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "AdvisoryConfidence",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "AdvisoryReasonSummary",
                table: "control_execution_records");
        }
    }
}

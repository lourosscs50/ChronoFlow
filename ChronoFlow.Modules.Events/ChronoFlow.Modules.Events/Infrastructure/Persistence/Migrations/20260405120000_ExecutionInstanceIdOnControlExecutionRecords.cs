using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionInstanceIdOnControlExecutionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionInstanceId",
                table: "control_execution_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_execution_records_ExecutionInstanceId",
                table: "control_execution_records",
                column: "ExecutionInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_control_execution_records_ExecutionInstanceId",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "ExecutionInstanceId",
                table: "control_execution_records");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5OperatorReviewAndOccurredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OccurredAtUtc",
                table: "control_execution_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorReviewAction",
                table: "control_execution_records",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OperatorReviewActionAtUtc",
                table: "control_execution_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorReviewNote",
                table: "control_execution_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "OperatorReviewAction",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "OperatorReviewActionAtUtc",
                table: "control_execution_records");

            migrationBuilder.DropColumn(
                name: "OperatorReviewNote",
                table: "control_execution_records");
        }
    }
}

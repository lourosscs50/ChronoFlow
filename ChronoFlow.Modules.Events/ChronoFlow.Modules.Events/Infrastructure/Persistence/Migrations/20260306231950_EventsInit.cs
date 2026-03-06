using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChronoFlow.Modules.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventsInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_EventType",
                table: "events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_events_OccurredAtUtc",
                table: "events",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_events_StreamId",
                table: "events",
                column: "StreamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");
        }
    }
}

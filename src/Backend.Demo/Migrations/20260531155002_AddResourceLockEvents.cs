using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceLockEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredAt",
                table: "ResourceDetail",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "ExecutableId",
                table: "ResourceDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ExecutableType",
                table: "ResourceDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NodeId",
                table: "ResourceDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResourceLockEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutableType = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutableId = table.Column<long>(type: "INTEGER", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", nullable: true),
                    ResourceType = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FlowTaskDetailId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLockEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceLockEvent_FlowTaskDetail_FlowTaskDetailId",
                        column: x => x.FlowTaskDetailId,
                        principalTable: "FlowTaskDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLockEvent_FlowTaskDetailId",
                table: "ResourceLockEvent",
                column: "FlowTaskDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceLockEvent");

            migrationBuilder.DropColumn(
                name: "AcquiredAt",
                table: "ResourceDetail");

            migrationBuilder.DropColumn(
                name: "ExecutableId",
                table: "ResourceDetail");

            migrationBuilder.DropColumn(
                name: "ExecutableType",
                table: "ResourceDetail");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "ResourceDetail");
        }
    }
}

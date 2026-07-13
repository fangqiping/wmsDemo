using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleOccurrence",
                table: "ResourceDetail",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleOccurrence",
                table: "OperationTaskDetail",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchedulePlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousPlanId = table.Column<long>(type: "INTEGER", nullable: true),
                    HorizonStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HorizonEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SolverStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerDetail = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Makespan = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleSolveAttempt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PreviousPlanId = table.Column<long>(type: "INTEGER", nullable: true),
                    Trigger = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerDetail = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SolverStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    CandidateCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSolveAttempt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulePlanHead",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPlanId = table.Column<long>(type: "INTEGER", nullable: true),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePlanHead", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchedulePlanHead_SchedulePlan_CurrentPlanId",
                        column: x => x.CurrentPlanId,
                        principalTable: "SchedulePlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchedulePlanItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemKind = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Occurrence = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OccupancyIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    PlannedStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlannedEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PredictedEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpectedDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayReason = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsFrozen = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayLabel = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    DisplayContextJson = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: true),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    LastFeedbackAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePlanItem", x => x.Id);
                    table.UniqueConstraint("AK_SchedulePlanItem_Id_PlanId", x => new { x.Id, x.PlanId });
                    table.ForeignKey(
                        name: "FK_SchedulePlanItem_SchedulePlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SchedulePlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeScheduleFeedback",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<long>(type: "INTEGER", nullable: false),
                    PlanItemId = table.Column<long>(type: "INTEGER", nullable: false),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Occurrence = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeScheduleFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuntimeScheduleFeedback_SchedulePlanItem_PlanItemId_PlanId",
                        columns: x => new { x.PlanItemId, x.PlanId },
                        principalTable: "SchedulePlanItem",
                        principalColumns: new[] { "Id", "PlanId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RuntimeScheduleFeedback_SchedulePlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SchedulePlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeScheduleFeedback_PlanId",
                table: "RuntimeScheduleFeedback",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeScheduleFeedback_PlanItemId_EventType_OccurredAt",
                table: "RuntimeScheduleFeedback",
                columns: new[] { "PlanItemId", "EventType", "OccurredAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeScheduleFeedback_PlanItemId_PlanId",
                table: "RuntimeScheduleFeedback",
                columns: new[] { "PlanItemId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlan_Status",
                table: "SchedulePlan",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlan_Version",
                table: "SchedulePlan",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlanHead_CurrentPlanId",
                table: "SchedulePlanHead",
                column: "CurrentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlanItem_PlanId_ItemKind_FlowTaskId_NodeId_Occurrence_OccupancyIndex",
                table: "SchedulePlanItem",
                columns: new[] { "PlanId", "ItemKind", "FlowTaskId", "NodeId", "Occurrence", "OccupancyIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchedulePlanItem_PlanId_ResourceType_ResourceId_PlannedStart_PlannedEnd",
                table: "SchedulePlanItem",
                columns: new[] { "PlanId", "ResourceType", "ResourceId", "PlannedStart", "PlannedEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSolveAttempt_StartedAt",
                table: "ScheduleSolveAttempt",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuntimeScheduleFeedback");

            migrationBuilder.DropTable(
                name: "SchedulePlanHead");

            migrationBuilder.DropTable(
                name: "ScheduleSolveAttempt");

            migrationBuilder.DropTable(
                name: "SchedulePlanItem");

            migrationBuilder.DropTable(
                name: "SchedulePlan");

            migrationBuilder.DropColumn(
                name: "ScheduleOccurrence",
                table: "ResourceDetail");

            migrationBuilder.DropColumn(
                name: "ScheduleOccurrence",
                table: "OperationTaskDetail");
        }
    }
}

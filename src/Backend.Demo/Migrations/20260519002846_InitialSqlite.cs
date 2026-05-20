using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsoleInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Acquired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowBinding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessType = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowDefinitionCode = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowBinding", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowDefinition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveVersionId = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowDraft",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    DraftDocumentJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowDraft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowTaskDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowId = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionOptionsDetail = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFlowTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    NodeId = table.Column<string>(type: "TEXT", nullable: true),
                    Acknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartingTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowTaskDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowVersion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RuntimeFlowId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceDraftRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceGraphJson = table.Column<string>(type: "TEXT", nullable: false),
                    CompiledGraphJson = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    FlowDefinitionCode = table.Column<string>(type: "TEXT", nullable: true),
                    FlowVersionNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: true),
                    Confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ConfirmedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationTaskDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConsoleId = table.Column<string>(type: "TEXT", nullable: true),
                    OperationTaskType = table.Column<string>(type: "TEXT", nullable: false),
                    CustomProperties = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFlowTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    NodeId = table.Column<string>(type: "TEXT", nullable: true),
                    Acknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartingTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationTaskDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Destination = table.Column<string>(type: "TEXT", nullable: false),
                    FlowDefinitionCode = table.Column<string>(type: "TEXT", nullable: true),
                    FlowVersionNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sku",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Spec = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sku", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceDetail_FlowTaskDetail_FlowTaskId",
                        column: x => x.FlowTaskId,
                        principalTable: "FlowTaskDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariableEntity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FlowTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableEntity", x => new { x.Id, x.FlowTaskId });
                    table.ForeignKey(
                        name: "FK_VariableEntity_FlowTaskDetail_FlowTaskId",
                        column: x => x.FlowTaskId,
                        principalTable: "FlowTaskDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LocationType = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Location_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InboundOrderLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InboundOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    TargetLocationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundOrderLine_InboundOrder_InboundOrderId",
                        column: x => x.InboundOrderId,
                        principalTable: "InboundOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboundOrderLine_Location_TargetLocationId",
                        column: x => x.TargetLocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboundOrderLine_Sku_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Sku",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboundOrderLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutboundOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    SourceLocationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundOrderLine_Location_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutboundOrderLine_OutboundOrder_OutboundOrderId",
                        column: x => x.OutboundOrderId,
                        principalTable: "OutboundOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutboundOrderLine_Sku_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Sku",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderLine_InboundOrderId",
                table: "InboundOrderLine",
                column: "InboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderLine_SkuId",
                table: "InboundOrderLine",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundOrderLine_TargetLocationId",
                table: "InboundOrderLine",
                column: "TargetLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_WarehouseId",
                table: "Location",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderLine_OutboundOrderId",
                table: "OutboundOrderLine",
                column: "OutboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderLine_SkuId",
                table: "OutboundOrderLine",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundOrderLine_SourceLocationId",
                table: "OutboundOrderLine",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceDetail_FlowTaskId",
                table: "ResourceDetail",
                column: "FlowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableEntity_FlowTaskId",
                table: "VariableEntity",
                column: "FlowTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsoleInfo");

            migrationBuilder.DropTable(
                name: "FlowBinding");

            migrationBuilder.DropTable(
                name: "FlowDefinition");

            migrationBuilder.DropTable(
                name: "FlowDraft");

            migrationBuilder.DropTable(
                name: "FlowVersion");

            migrationBuilder.DropTable(
                name: "InboundOrderLine");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "OperationTaskDetail");

            migrationBuilder.DropTable(
                name: "OutboundOrderLine");

            migrationBuilder.DropTable(
                name: "ResourceDetail");

            migrationBuilder.DropTable(
                name: "VariableEntity");

            migrationBuilder.DropTable(
                name: "InboundOrder");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "OutboundOrder");

            migrationBuilder.DropTable(
                name: "Sku");

            migrationBuilder.DropTable(
                name: "FlowTaskDetail");

            migrationBuilder.DropTable(
                name: "Warehouse");
        }
    }
}

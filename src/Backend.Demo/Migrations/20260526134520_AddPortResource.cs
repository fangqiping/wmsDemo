using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddPortResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Port",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Acquired = table.Column<bool>(type: "INTEGER", nullable: false),
                    PortType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPalletId = table.Column<int>(type: "INTEGER", nullable: true),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Port", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Port_Pallet_CurrentPalletId",
                        column: x => x.CurrentPalletId,
                        principalTable: "Pallet",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Port_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Port_CurrentPalletId",
                table: "Port",
                column: "CurrentPalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Port_WarehouseId",
                table: "Port",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Port");
        }
    }
}

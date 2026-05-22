using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddPalletResourceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentPalletId",
                table: "Location",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pallet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Acquired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pallet_Sku_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Sku",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Location_CurrentPalletId",
                table: "Location",
                column: "CurrentPalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallet_SkuId",
                table: "Pallet",
                column: "SkuId");

            migrationBuilder.AddForeignKey(
                name: "FK_Location_Pallet_CurrentPalletId",
                table: "Location",
                column: "CurrentPalletId",
                principalTable: "Pallet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Location_Pallet_CurrentPalletId",
                table: "Location");

            migrationBuilder.DropTable(
                name: "Pallet");

            migrationBuilder.DropIndex(
                name: "IX_Location_CurrentPalletId",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "CurrentPalletId",
                table: "Location");
        }
    }
}

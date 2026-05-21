using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationResourceState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Acquired",
                table: "Location",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Location",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Location",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acquired",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Location");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Location");
        }
    }
}

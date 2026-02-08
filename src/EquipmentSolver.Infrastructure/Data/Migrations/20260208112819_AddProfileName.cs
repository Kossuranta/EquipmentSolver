using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentSolver.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "GameProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "GameProfiles");
        }
    }
}

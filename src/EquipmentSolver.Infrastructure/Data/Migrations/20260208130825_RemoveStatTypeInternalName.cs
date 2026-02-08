using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentSolver.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStatTypeInternalName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatTypes_ProfileId_Name",
                table: "StatTypes");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "StatTypes");

            migrationBuilder.CreateIndex(
                name: "IX_StatTypes_ProfileId_DisplayName",
                table: "StatTypes",
                columns: new[] { "ProfileId", "DisplayName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatTypes_ProfileId_DisplayName",
                table: "StatTypes");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "StatTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StatTypes_ProfileId_Name",
                table: "StatTypes",
                columns: new[] { "ProfileId", "Name" },
                unique: true);
        }
    }
}

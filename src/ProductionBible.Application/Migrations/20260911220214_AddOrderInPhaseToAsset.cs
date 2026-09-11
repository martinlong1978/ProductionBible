using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionBible.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInPhaseToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderInPhase",
                table: "Assets",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderInPhase",
                table: "Assets");
        }
    }
}

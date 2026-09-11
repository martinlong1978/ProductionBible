using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionBible.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInBeatToAssetBeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderInBeat",
                table: "AssetBeats",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderInBeat",
                table: "AssetBeats");
        }
    }
}

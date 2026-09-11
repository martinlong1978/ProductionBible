using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionBible.Application.Migrations
{
    /// <inheritdoc />
    public partial class RenameBeatTimecodeAddOrdinalAndDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timecode",
                table: "Beats",
                newName: "SourceTimecode");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Beats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ordinal",
                table: "Beats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Beats");

            migrationBuilder.DropColumn(
                name: "Ordinal",
                table: "Beats");

            migrationBuilder.RenameColumn(
                name: "SourceTimecode",
                table: "Beats",
                newName: "Timecode");
        }
    }
}

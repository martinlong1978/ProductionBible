using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionBible.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseEntityAndAssetPhaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhaseId",
                table: "Assets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Phases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Phases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_PhaseId",
                table: "Assets",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Phases_ProjectId",
                table: "Phases",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Phases_PhaseId",
                table: "Assets",
                column: "PhaseId",
                principalTable: "Phases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Phases_PhaseId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "Phases");

            migrationBuilder.DropIndex(
                name: "IX_Assets_PhaseId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PhaseId",
                table: "Assets");
        }
    }
}

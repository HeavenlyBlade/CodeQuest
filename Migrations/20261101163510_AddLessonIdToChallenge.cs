using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeQuest.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonIdToChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Challenges",
                type: "integer",
                nullable: true);

            // Wire existing seeded challenges to their paired lessons by matching OrderNumber within the zone.
            // This runs once on existing databases that were seeded before this migration.
            migrationBuilder.Sql(@"
                UPDATE ""Challenges"" c
                SET ""LessonId"" = l.""Id""
                FROM ""Lessons"" l
                WHERE l.""ZoneId"" = c.""ZoneId""
                  AND l.""OrderNumber"" = c.""OrderNumber""
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_LessonId",
                table: "Challenges",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Lessons_LessonId",
                table: "Challenges",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Lessons_LessonId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_LessonId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Challenges");
        }
    }
}

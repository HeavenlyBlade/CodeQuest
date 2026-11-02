using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeQuest.Migrations
{
    /// <inheritdoc />
    public partial class WipeAndReseedChapterOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Attempts"";
                DELETE FROM ""Battles"";
                DELETE FROM ""LessonProgresses"";
                DELETE FROM ""Questions"";
                DELETE FROM ""Challenges"";
                DELETE FROM ""Lessons"";
                DELETE FROM ""Zones"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible wipe — no rollback
        }
    }
}

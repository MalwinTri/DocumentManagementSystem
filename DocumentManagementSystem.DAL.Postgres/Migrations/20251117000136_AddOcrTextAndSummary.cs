using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrTextAndSummary : Migration
    {
        /// <inheritdoc />
        //protected override void Up(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.AddColumn<string>(
        //        name: "OcrText",
        //        table: "Documents",
        //        type: "text",
        //        nullable: true);

        //    migrationBuilder.AddColumn<string>(
        //        name: "Summary",
        //        table: "Documents",
        //        type: "text",
        //        nullable: true);
        //}

        ///// <inheritdoc />
        //protected override void Down(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropColumn(
        //        name: "OcrText",
        //        table: "Documents");

        //    migrationBuilder.DropColumn(
        //        name: "Summary",
        //        table: "Documents");
        //}
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""OcrText"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""Summary"" text;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" DROP COLUMN IF EXISTS ""OcrText"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" DROP COLUMN IF EXISTS ""Summary"";");
        }

    }
}

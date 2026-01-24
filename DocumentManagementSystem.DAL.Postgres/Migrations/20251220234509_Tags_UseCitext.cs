using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentManagementSystem.Migrations
{
    public partial class Tags_UseCitext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure extension exists
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS citext;");

            // Drop old unique index (btree on varchar)
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            // Alter column to citext
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            // Recreate unique index (now case-insensitive because citext)
            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }
    }
}

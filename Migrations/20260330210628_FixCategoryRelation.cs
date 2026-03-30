using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HORIZON1.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Categories_CategoryId1",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CategoryId1",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_CategoryId1",
                table: "Events",
                column: "CategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Categories_CategoryId1",
                table: "Events",
                column: "CategoryId1",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}

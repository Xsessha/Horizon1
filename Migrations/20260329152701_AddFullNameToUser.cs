using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HORIZON1.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSent",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "RemindAt",
                table: "Reminders",
                newName: "ReminderTime");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "AspNetUsers",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Reminders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "Categories",
                type: "varchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Categories_CategoryId1",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CategoryId1",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "ReminderTime",
                table: "Reminders",
                newName: "RemindAt");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                table: "Reminders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "Categories",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}

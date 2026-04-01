using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HORIZON1.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrenceEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "Events",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Events");
        }
    }
}

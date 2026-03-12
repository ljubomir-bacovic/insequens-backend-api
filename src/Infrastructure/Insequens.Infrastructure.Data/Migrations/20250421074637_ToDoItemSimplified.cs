using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Journal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ToDoItemSimplified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ToDoItem");

            migrationBuilder.DropColumn(
                name: "ExactDateTime",
                table: "ToDoItem");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DueDate",
                table: "ToDoItem",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ToDoItem",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "ToDoItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExactDateTime",
                table: "ToDoItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

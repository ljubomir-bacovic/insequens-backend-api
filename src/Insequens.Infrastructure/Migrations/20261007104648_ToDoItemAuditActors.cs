using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insequens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ToDoItemAuditActors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ToDoItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "ToDoItem",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ToDoItem");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ToDoItem");
        }
    }
}

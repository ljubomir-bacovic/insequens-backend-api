using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insequens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_CreatedOn",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_IsCompleted_DueDate",
                table: "Tasks");

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DeletedOn",
                table: "Tasks",
                column: "DeletedOn",
                filter: "[IsDeleted] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_IsDeleted_CreatedOn",
                table: "Tasks",
                columns: new[] { "UserId", "IsDeleted", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_IsDeleted_IsCompleted_DueDate",
                table: "Tasks",
                columns: new[] { "UserId", "IsDeleted", "IsCompleted", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_DeletedOn",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_IsDeleted_CreatedOn",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_IsDeleted_IsCompleted_DueDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_CreatedOn",
                table: "Tasks",
                columns: new[] { "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_IsCompleted_DueDate",
                table: "Tasks",
                columns: new[] { "UserId", "IsCompleted", "DueDate" });
        }
    }
}

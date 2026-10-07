using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insequens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskSchemaHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added to the scaffold: shrinking the columns and adding the check constraint fail on rows that do
            // not fit. Stop with a clear message, inside the migration's transaction, before changing anything.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM ToDoItem WHERE DATALENGTH(Name) / 2 > 200)
                    THROW 50004, 'TaskSchemaHardening: a ToDoItem.Name is longer than 200 characters. Shorten it, then migrate again.', 1;
                IF EXISTS (SELECT 1 FROM ToDoItem WHERE DATALENGTH(Description) / 2 > 4000)
                    THROW 50005, 'TaskSchemaHardening: a ToDoItem.Description is longer than 4000 characters. Shorten it, then migrate again.', 1;
                IF EXISTS (SELECT 1 FROM ToDoItem WHERE Priority NOT IN (0, 1, 2, 3))
                    THROW 50006, 'TaskSchemaHardening: a ToDoItem.Priority is not 0, 1, 2 or 3. Correct it, then migrate again.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ToDoItem_AspNetUsers_UserId",
                table: "ToDoItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ToDoItem",
                table: "ToDoItem");

            migrationBuilder.DropIndex(
                name: "IX_ToDoItem_UserId",
                table: "ToDoItem");

            migrationBuilder.RenameTable(
                name: "ToDoItem",
                newName: "Tasks");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tasks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_CreatedOn",
                table: "Tasks",
                columns: new[] { "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_IsCompleted_DueDate",
                table: "Tasks",
                columns: new[] { "UserId", "IsCompleted", "DueDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks",
                sql: "[Priority] IN (0, 1, 2, 3)");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_CreatedOn",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_IsCompleted_DueDate",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tasks");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "ToDoItem");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ToDoItem",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ToDoItem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ToDoItem",
                table: "ToDoItem",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoItem_UserId",
                table: "ToDoItem",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ToDoItem_AspNetUsers_UserId",
                table: "ToDoItem",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

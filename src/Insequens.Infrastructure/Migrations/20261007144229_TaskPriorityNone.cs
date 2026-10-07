using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insequens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaskPriorityNone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added to the scaffold: the priority values are reordered to ascending importance (INS-030), so the
            // rows are remapped (High 1 -> 3, Low 3 -> 1, no priority NULL -> None 0) before the column becomes NOT
            // NULL. SQL Server cannot alter a column a check constraint reads, so the constraint is dropped and
            // re-added around the change.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks");

            migrationBuilder.Sql("""
                UPDATE Tasks SET Priority = CASE
                    WHEN Priority IS NULL THEN 0
                    WHEN Priority = 1 THEN 3
                    WHEN Priority = 3 THEN 1
                    ELSE Priority
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks",
                sql: "[Priority] IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks");

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Tasks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("""
                UPDATE Tasks SET Priority = CASE Priority
                    WHEN 0 THEN NULL
                    WHEN 1 THEN 3
                    WHEN 3 THEN 1
                    ELSE Priority
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks",
                sql: "[Priority] IN (0, 1, 2, 3)");
        }
    }
}

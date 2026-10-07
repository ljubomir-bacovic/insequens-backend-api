using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Insequens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a01"), "8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a01", "Admin", "ADMIN" },
                    { new Guid("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a02"), "8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a02", "Support", "SUPPORT" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a01"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a02"));
        }
    }
}

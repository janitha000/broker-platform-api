using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFactFind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FactFind_Assets",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FactFind_CompletedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactFind_Debts",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactFind_Expenses",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactFind_Income",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FactFind_Objectives",
                table: "Cases",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactFind_Assets",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FactFind_CompletedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FactFind_Debts",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FactFind_Expenses",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FactFind_Income",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FactFind_Objectives",
                table: "Cases");
        }
    }
}

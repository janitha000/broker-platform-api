using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseLifecycleSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseLifecycleState",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FactFindCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseLifecycleState", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseLifecycleState_CurrentState",
                table: "CaseLifecycleState",
                column: "CurrentState");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseLifecycleState");
        }
    }
}

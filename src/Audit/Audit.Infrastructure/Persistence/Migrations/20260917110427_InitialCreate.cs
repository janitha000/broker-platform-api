using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Sensitivity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnvelopeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrevHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "TenantAuditHeads",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuditHeads", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_CaseId",
                table: "AuditEvents",
                columns: new[] { "TenantId", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "TenantAuditHeads");
        }
    }
}

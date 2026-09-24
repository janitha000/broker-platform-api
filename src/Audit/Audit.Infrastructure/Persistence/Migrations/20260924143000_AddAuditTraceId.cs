using Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audit.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuditDbContext))]
[Migration("20260924143000_AddAuditTraceId")]
public partial class AddAuditTraceId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TraceId",
            table: "AuditEvents",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TraceId", table: "AuditEvents");
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Origination.Infrastructure.Persistence;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OriginationDbContext))]
[Migration("20260924143000_AddOutboxTraceContext")]
public partial class AddOutboxTraceContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TraceParent",
            table: "OutboxMessages",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TraceState",
            table: "OutboxMessages",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TraceParent", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "TraceState", table: "OutboxMessages");
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Origination.Infrastructure.Persistence;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OriginationDbContext))]
[Migration("20260917120000_WidenOutboxPayload")]
public partial class WidenOutboxPayload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Payload",
            table: "OutboxMessages",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(4000)",
            oldMaxLength: 4000);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Payload",
            table: "OutboxMessages",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");
    }
}

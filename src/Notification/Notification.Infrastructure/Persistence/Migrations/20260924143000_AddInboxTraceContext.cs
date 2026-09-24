using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Notification.Infrastructure.Persistence;

#nullable disable

namespace Notification.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NotificationDbContext))]
[Migration("20260924143000_AddInboxTraceContext")]
public partial class AddInboxTraceContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.InboxMessages', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.InboxMessages (
                    Id uniqueidentifier NOT NULL PRIMARY KEY,
                    Type nvarchar(128) NOT NULL,
                    Payload nvarchar(4000) NOT NULL,
                    IdempotencyKey nvarchar(256) NOT NULL,
                    Status nvarchar(32) NOT NULL,
                    ReceivedAt datetime2 NOT NULL,
                    ProcessedAt datetime2 NULL,
                    NextAttemptAt datetime2 NOT NULL,
                    LockedUntil datetime2 NULL,
                    AttemptCount int NOT NULL,
                    LastError nvarchar(2000) NULL,
                    TraceParent nvarchar(128) NULL,
                    TraceState nvarchar(512) NULL
                );
                CREATE UNIQUE INDEX IX_InboxMessages_IdempotencyKey ON dbo.InboxMessages (IdempotencyKey);
                CREATE INDEX IX_InboxMessages_Status_NextAttemptAt ON dbo.InboxMessages (Status, NextAttemptAt);
            END
            ELSE
            BEGIN
                IF COL_LENGTH('dbo.InboxMessages', 'TraceParent') IS NULL
                    ALTER TABLE dbo.InboxMessages ADD TraceParent nvarchar(128) NULL;
                IF COL_LENGTH('dbo.InboxMessages', 'TraceState') IS NULL
                    ALTER TABLE dbo.InboxMessages ADD TraceState nvarchar(512) NULL;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.InboxMessages', 'TraceParent') IS NOT NULL
                ALTER TABLE dbo.InboxMessages DROP COLUMN TraceParent;
            IF COL_LENGTH('dbo.InboxMessages', 'TraceState') IS NOT NULL
                ALTER TABLE dbo.InboxMessages DROP COLUMN TraceState;
            """);
    }
}

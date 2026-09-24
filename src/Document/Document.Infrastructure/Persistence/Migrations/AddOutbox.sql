-- Run against database Document if you created tables without EF migrations.
IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessages (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        Type nvarchar(128) NOT NULL,
        Payload nvarchar(max) NOT NULL,
        IdempotencyKey nvarchar(256) NOT NULL,
        OccurredAt datetime2 NOT NULL,
        PublishedAt datetime2 NULL,
        TraceParent nvarchar(128) NULL,
        TraceState nvarchar(512) NULL
    );
    CREATE UNIQUE INDEX IX_OutboxMessages_IdempotencyKey ON dbo.OutboxMessages (IdempotencyKey);
    CREATE INDEX IX_OutboxMessages_PublishedAt ON dbo.OutboxMessages (PublishedAt);
END

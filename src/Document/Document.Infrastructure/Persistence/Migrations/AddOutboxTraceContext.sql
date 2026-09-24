-- Existing Document.OutboxMessages tables created from AddOutbox.sql
IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.OutboxMessages', 'TraceParent') IS NULL
        ALTER TABLE dbo.OutboxMessages ADD TraceParent nvarchar(128) NULL;
    IF COL_LENGTH('dbo.OutboxMessages', 'TraceState') IS NULL
        ALTER TABLE dbo.OutboxMessages ADD TraceState nvarchar(512) NULL;
END

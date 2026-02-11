-- Source: src/transactions/Maomi.MQ.Transaction.SqlServer/SqlServerTransactionDatabaseProvider.cs
-- Method: EnsureTablesExistAsync
-- Default table names: mq_publisher, mq_consumer

IF OBJECT_ID(N'[mq_publisher]', N'U') IS NULL
BEGIN
    CREATE TABLE [mq_publisher] (
      [message_id] nvarchar(64) NOT NULL,
      [exchange] nvarchar(256) NOT NULL,
      [routing_key] nvarchar(256) NOT NULL,
      [message_header] nvarchar(max) NOT NULL,
      [message_body] nvarchar(max) NOT NULL,
      [message_text] nvarchar(max) NOT NULL,
      [status] int NOT NULL,
      [retry_count] int NOT NULL,
      [next_retry_time] datetimeoffset(7) NOT NULL,
      [lock_id] nvarchar(128) NOT NULL,
      [lock_time] datetimeoffset(7) NULL,
      [last_error] nvarchar(max) NOT NULL,
      [create_time] datetimeoffset(7) NOT NULL,
      [update_time] datetimeoffset(7) NOT NULL,
      CONSTRAINT [PK_mq_publisher] PRIMARY KEY CLUSTERED ([message_id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_mq_publisher_status_next_retry' AND object_id = OBJECT_ID(N'[mq_publisher]'))
BEGIN
    CREATE INDEX [IX_mq_publisher_status_next_retry] ON [mq_publisher]([status], [next_retry_time]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_mq_publisher_lock_time' AND object_id = OBJECT_ID(N'[mq_publisher]'))
BEGIN
    CREATE INDEX [IX_mq_publisher_lock_time] ON [mq_publisher]([lock_time]);
END;

IF OBJECT_ID(N'[mq_consumer]', N'U') IS NULL
BEGIN
    CREATE TABLE [mq_consumer] (
      [consumer_name] nvarchar(200) NOT NULL,
      [message_id] nvarchar(64) NOT NULL,
      [message_header] nvarchar(max) NOT NULL,
      [exchange] nvarchar(256) NOT NULL,
      [routing_key] nvarchar(256) NOT NULL,
      [status] int NOT NULL,
      [lock_id] nvarchar(128) NOT NULL,
      [lock_time] datetimeoffset(7) NULL,
      [last_error] nvarchar(max) NOT NULL,
      [create_time] datetimeoffset(7) NOT NULL,
      [update_time] datetimeoffset(7) NOT NULL,
      CONSTRAINT [PK_mq_consumer] PRIMARY KEY CLUSTERED ([consumer_name], [message_id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_mq_consumer_status_lock_time' AND object_id = OBJECT_ID(N'[mq_consumer]'))
BEGIN
    CREATE INDEX [IX_mq_consumer_status_lock_time] ON [mq_consumer]([status], [lock_time]);
END;

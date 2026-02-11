-- Source: src/transactions/Maomi.MQ.Transaction.Postgres/PostgresTransactionDatabaseProvider.cs
-- Method: EnsureTablesExistAsync
-- Default table names: mq_publisher, mq_consumer

CREATE TABLE IF NOT EXISTS "mq_publisher" (
  message_id varchar(64) NOT NULL,
  exchange varchar(256) NOT NULL,
  routing_key varchar(256) NOT NULL,
  message_header text NOT NULL,
  message_body text NOT NULL,
  message_text text NOT NULL,
  status int NOT NULL,
  retry_count int NOT NULL,
  next_retry_time timestamptz NOT NULL,
  lock_id varchar(128) NOT NULL,
  lock_time timestamptz NULL,
  last_error text NOT NULL,
  create_time timestamptz NOT NULL,
  update_time timestamptz NOT NULL,
  PRIMARY KEY (message_id)
);

CREATE INDEX IF NOT EXISTS ix_outbox_status_next_retry ON "mq_publisher" (status, next_retry_time);

CREATE INDEX IF NOT EXISTS ix_outbox_lock_time ON "mq_publisher" (lock_time);

CREATE TABLE IF NOT EXISTS "mq_consumer" (
  consumer_name varchar(200) NOT NULL,
  message_id varchar(64) NOT NULL,
  message_header text NOT NULL,
  exchange varchar(256) NOT NULL,
  routing_key varchar(256) NOT NULL,
  status int NOT NULL,
  lock_id varchar(128) NOT NULL,
  lock_time timestamptz NULL,
  last_error text NOT NULL,
  create_time timestamptz NOT NULL,
  update_time timestamptz NOT NULL,
  PRIMARY KEY (consumer_name, message_id)
);

CREATE INDEX IF NOT EXISTS ix_inbox_status_lock_time ON "mq_consumer" (status, lock_time);

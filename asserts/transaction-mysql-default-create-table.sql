-- Source: src/transactions/Maomi.MQ.Transaction.Mysql/MySqlTransactionDatabaseProvider.cs
-- Method: EnsureTablesExistAsync
-- Default table names: mq_publisher, mq_consumer

CREATE TABLE IF NOT EXISTS `mq_publisher` (
  `message_id` varchar(64) NOT NULL,
  `exchange` varchar(256) NOT NULL,
  `routing_key` varchar(256) NOT NULL,
  `message_header` longtext NOT NULL,
  `message_body` longtext NOT NULL,
  `message_text` longtext NOT NULL,
  `status` int NOT NULL,
  `retry_count` int NOT NULL,
  `next_retry_time` datetime(6) NOT NULL,
  `lock_id` varchar(128) NOT NULL,
  `lock_time` datetime(6) NULL,
  `last_error` longtext NOT NULL,
  `create_time` datetime(6) NOT NULL,
  `update_time` datetime(6) NOT NULL,
  PRIMARY KEY (`message_id`),
  INDEX `ix_outbox_status_next_retry` (`status`, `next_retry_time`),
  INDEX `ix_outbox_lock_time` (`lock_time`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `mq_consumer` (
  `consumer_name` varchar(200) NOT NULL,
  `message_id` varchar(64) NOT NULL,
  `message_header` longtext NOT NULL,
  `exchange` varchar(256) NOT NULL,
  `routing_key` varchar(256) NOT NULL,
  `status` int NOT NULL,
  `lock_id` varchar(128) NOT NULL,
  `lock_time` datetime(6) NULL,
  `last_error` longtext NOT NULL,
  `create_time` datetime(6) NOT NULL,
  `update_time` datetime(6) NOT NULL,
  PRIMARY KEY (`consumer_name`, `message_id`),
  INDEX `ix_inbox_status_lock_time` (`status`, `lock_time`)
) ENGINE=InnoDB;

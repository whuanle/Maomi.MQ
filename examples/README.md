# Maomi.MQ Examples (Refactored)

This folder contains the rebuilt sample projects for `Maomi.MQ`.

## Structure

- `00-ScenarioHub/Maomi.MQ.Examples.ScenarioHub.Api` (**recommended**)
  - One project with multiple controllers and runtime status APIs.
  - Covers quickstart/eventbus/dynamic/retry-deadletter/protobuf/batch scenarios.
  - Best choice for API-triggered end-to-end verification.
- `01-QuickStart/Maomi.MQ.Examples.QuickStart.Api`
  - Minimal API + `IMessagePublisher.AutoPublishAsync` + fixed consumer.
- `02-EventBus/Maomi.MQ.Examples.EventBus.Api`
  - EventBus middleware + ordered handlers (`[EventOrder]`).
- `03-DynamicConsumer/Maomi.MQ.Examples.DynamicConsumer.Api`
  - Start/stop dynamic consumers at runtime via `IDynamicConsumer`.
- `04-RetryDeadLetter/Maomi.MQ.Examples.RetryDeadLetter.Api`
  - Retry + fallback + explicit dead-letter forwarding sample.
- `05-ProtobufWorker/Maomi.MQ.Examples.Protobuf.Worker`
  - `protobuf-net` serializer sample with producer + consumer.
- `06-BatchPublisher/Maomi.MQ.Examples.BatchPublisher.Worker`
  - New batch publishing worker sample (high-frequency producer scenario).
- `07-Transaction/Maomi.MQ.Examples.Transaction.Api`
  - Outbox + inbox barrier sample based on `Maomi.MQ.Transaction`.
  - Includes API controller for manual outbox registration in DB transaction, then publish after commit.

## Run

1. Ensure RabbitMQ is available.
2. Set env var (optional): `RABBITMQ=127.0.0.1`
3. Build all samples:

```bash
dotnet build examples/Maomi.MQ.Examples.sln
```

4. Run one sample:

```bash
dotnet run --project examples/00-ScenarioHub/Maomi.MQ.Examples.ScenarioHub.Api
```

## ScenarioHub APIs

- Status and health
  - `GET /api/scenario/status`
  - `POST /api/scenario/status/reset`
- QuickStart
  - `POST /api/scenario/quickstart/publish`
- EventBus
  - `POST /api/scenario/eventbus/publish`
- Dynamic consumer
  - `POST /api/scenario/dynamic/start`
  - `DELETE /api/scenario/dynamic/stop/{queue}`
  - `POST /api/scenario/dynamic/publish`
- Retry + dead letter
  - `POST /api/scenario/retry/publish`
- Protobuf
  - `POST /api/scenario/protobuf/publish`
- Batch publisher
  - `POST /api/scenario/batch/publish-once`
  - `POST /api/scenario/batch/worker/start`
  - `POST /api/scenario/batch/worker/stop`

## Transaction APIs

- Register outbox row in DB transaction, commit, then publish message
  - `POST /api/transaction/publish`
- Query business table rows
  - `GET /api/transaction/orders?take=20`
- Query service status
  - `GET /api/transaction/status`

## Notes

- Sample project names are unified under `Maomi.MQ.Examples.*`.
- Projects target `net8.0` and rely on central package management.
- Swagger is enabled for API samples in Development environment.

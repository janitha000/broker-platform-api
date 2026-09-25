# Broker.Contracts

Shared message types for the MassTransit learning slice (Origination → Notification).

Keep this project free of MassTransit, EF, and AWS packages. Both publishers and consumers reference **types only**.

## Step 1

`CaseFactFindCompleted` consumer on Notification. Tests use the in-memory test harness.

## Step 2 (current)

Origination `OutboxPublisher` **publishes** `CaseFactFindCompleted` on MassTransit when `MassTransit:Transport` is `RabbitMq`. Audit and other outbox types still use `IMessageBus` (Logging / EventBridge).

Notification uses the same Rabbit host and `ConfigureEndpoints`. The SQS fact-find worker does **not** start on RabbitMq (avoids two emails).

Local:

```bash
docker compose -f docker-compose.rabbitmq.yml up -d
```

Development `appsettings.Development.json` sets `Transport` to `RabbitMq` (guest/guest, `localhost`). Management UI: http://localhost:15672

Without Rabbit, set `Transport` to `InMemory` — fact-find stays on the old outbox bus; the Notification consumer is process-local only.

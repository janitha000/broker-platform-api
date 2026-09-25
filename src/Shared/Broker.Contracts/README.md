# Broker.Contracts

Shared message types for the MassTransit learning slice (Origination → Notification).

Keep this project free of MassTransit, EF, and AWS packages. Both publishers and consumers reference **types only**.

## Step 1

`CaseFactFindCompleted` consumer on Notification. Tests use the in-memory test harness.

## Step 2

Both APIs share RabbitMQ. Notification consumes `CaseFactFindCompleted`. SQS worker is off when `MassTransit:Transport` is `RabbitMq`.

## Step 3

When RabbitMq is on, `CompleteFactFindHandler` calls `IPublishEndpoint.Publish` **before** `SaveChanges`. MassTransit’s EF **bus outbox** stores the message on `OriginationDbContext` in that same commit, then delivers to Rabbit. Your JSON `OutboxMessages` row is only an idempotency marker (`PublishedAt` already set). Audit still uses the custom outbox + `OutboxPublisher`.

Apply Origination EF migrations so `InboxState`, `OutboxMessage`, and `OutboxState` exist.

## Step 4

Notification receive endpoints use `UseMessageRetry`: **3 immediate retries** for `EmailDeliveryFailedException` (SES/mock send failed). `NotificationTemplateNotFoundException` is **ignored** by retry (faults on first failure). After retries are exhausted, RabbitMQ places the message on the endpoint **`_error`** queue (`CaseFactFindCompleted_error`). Inspect it in the management UI. The old `InboxDispatcher` is not used on this path.

## Step 5 (current)

Origination hosts a MassTransit **state machine** (`CaseLifecycle`) correlated by `CaseId`. `CreateCase` publishes `CaseOpened` (bus outbox, same as fact-find). The saga starts in **Enquiry**, then `CaseFactFindCompleted` moves it to **FactFindCompleted**. Email still goes to the Notification consumer (choreography). The `Cases` table remains the board source of truth; the saga is process state. Apply Origination EF migrations so `CaseLifecycleState` exists. This is not `RegistrationSaga`.

# Broker.Contracts

Shared message types for the MassTransit learning slice (Origination → Notification).

Keep this project free of MassTransit, EF, and AWS packages. Both publishers and consumers reference **types only**.

## Step 1 (current)

`CaseFactFindCompleted` is consumed in Notification over MassTransit **in-memory**. Origination still writes the event as JSON to the transactional outbox / EventBridge. The SQS + inbox path is unchanged.

Next step: Origination `IPublishEndpoint.Publish` of this type (Rabbit or SQS), then retire the string `IMessageBus` for this event only.

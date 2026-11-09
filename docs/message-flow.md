# Message flow

The sequence diagrams in [order-flow.md](order-flow.md) are the main reference. This page lists every message and the rules that apply to all of them.

## Events and commands

- An **event** says something happened, in the past tense (`StockReserved`). It is published, and anyone interested can subscribe. The sender does not know who listens.
- A **command** tells one specific service to do one thing (`ChargePayment`). It is sent to one queue, and exactly one service handles it.

| Message | Kind | Sent by | Handled by | Fields |
|---|---|---|---|---|
| OrderSubmitted | event | Orders API (outbox) | Inventory, Orders saga | OrderId, SubmittedAtUtc, Items (Sku, Quantity), Total |
| StockReserved | event | Inventory | Orders saga | OrderId |
| StockReservationFailed | event | Inventory | Orders saga | OrderId, Reason |
| ChargePayment | command, queue `ChargePayment` | Orders saga | Payments | OrderId, Amount |
| PaymentCompleted | event | Payments | Orders saga | OrderId |
| PaymentFailed | event | Payments | Orders saga | OrderId, Reason |
| ReleaseStock | command, queue `ReleaseStock` | Orders saga | Inventory | OrderId, Items (Sku, Quantity) |
| StockReleased | event | Inventory | Orders saga | OrderId |
| OrderConfirmed | event | Orders saga | Orders `OrderStatusConsumer` | OrderId |
| OrderCancelled | event | Orders saga | Orders `OrderStatusConsumer` | OrderId, Reason |
| OrderFailed | event | Orders saga | Orders `OrderStatusConsumer` | OrderId, Reason |

All message records live in `src/Contracts`. Changing a field's name or order breaks the other services, as the Session 32 experiment showed. Add new fields at the end, and update every sender.

## Rules for every message

1. **Outbox on send.** No service publishes straight to RabbitMQ from business code. Messages go into the `OutboxMessage` table in the same transaction as the data, and a background process delivers them.
2. **Transaction around each consumer.** A consumer's database changes and its outgoing messages commit together, or both roll back.
3. **Inbox on receive.** `InboxState` records handled messages, so a redelivered message is skipped. Payments also keeps `processed_messages` for `ChargePayment`.
4. **Retries, then the error queue.** Five retries with growing waits. After that the message moves to `<queue>_error` for a human to inspect.
5. **At-least-once.** Any message can arrive twice. The inbox, the saga's ignore rules and the domain guards make that harmless.

## Seeing it live

- RabbitMQ management page: http://localhost:15672. Look at Exchanges (one per message type), Queues (one per consumer, plus the saga's queue), and the `_error` queues.
- To follow one order across services: `docker compose logs -f --since 5m orders-api inventory-api payments-api`, then search for the order id.

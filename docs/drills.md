# Failure drills

A drill means killing part of the system on purpose in the middle of an order, then checking that the order still completes **exactly once**. `docker compose kill` is a hard kill, like pulling the power cable. Write your prediction before each drill, then the result.

> Fill in the Actual column with what you saw. If a drill found a problem, write the problem down. A drill that finds something is a successful drill.

| Drill | What is killed | When | Expected | Actual |
|---|---|---|---|---|
| A | payments-api | before the charge | the order waits in AwaitingPayment and `ChargePayment` waits in its queue (Ready = 1); after `start` the order is Confirmed with one payment row | |
| B | orders-api (the saga itself) | while the saga waits for payment | `PaymentCompleted` waits in the saga's queue; after `start` the saga loads its row, moves to Confirmed, and the order row is Confirmed | |
| C | rabbitmq | before the order is placed | POST still returns 201 and the event waits in `OutboxMessage`; after `start` the outbox delivers it and the order is Confirmed | |

## How to run them

Start from a clean, seeded system: `docker compose down -v`, `docker compose up --build -d`, then `inventory.http` requests 20 and 21. Place one normal order first, so you know the system is healthy.

**Drill A.** Run `docker compose kill payments-api`, place an order (request 02), and check that it shows Pending with the saga in AwaitingPayment. Then run `docker compose start payments-api` and wait a few seconds.

**Drill B.** Run `docker compose stop payments-api` and place an order. Wait until the saga row is in AwaitingPayment. Run `docker compose kill orders-api`, then `docker compose start payments-api`. Check that the saga queue shows Ready = 1, then run `docker compose start orders-api`.

**Drill C.** Run `docker compose kill rabbitmq` and place an order (it still returns 201). Check that `SELECT count(*) FROM "OutboxMessage";` returns 1. Run `docker compose start rabbitmq` and wait 20 to 30 seconds.

## Proving "exactly once"

For the order id from each drill, check:

- **Orders database.** `SELECT "CurrentState" FROM order_states WHERE "CorrelationId" = '<id>';` returns one row, Confirmed.
- **Payments database.** `SELECT "OrderId", "Status" FROM payments WHERE "OrderId" = '<id>';` returns exactly one row.
- **Inventory.** Keyboard stock dropped by the order's quantity once, not twice.

## Why it works

- Messages wait in RabbitMQ queues while a consumer is down (A, B).
- Events wait in the sender's outbox while the broker is down (C).
- The saga's progress is in the database, not in memory (B).
- Redelivered messages are skipped by the inbox, and duplicates are ignored by the saga.

## Still to drill later

The tracker's Week 18 drills add stopping a database and cutting off the AI provider.

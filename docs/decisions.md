# Decision records

Short records of the important choices. Each has a context (the problem), a decision (what was chosen), and consequences (what it gains and what it costs).

## 1. Each service owns its database

- **Context.** One shared database would couple the services through their tables.
- **Decision.** Inventory, Orders and Payments each have their own PostgreSQL database and EF Core migrations. No service reads another's tables.
- **Consequences.** Each service can change its schema independently, and failures are isolated. On the other hand, there are no JOINs or transactions across services, so consistency needs messaging patterns (records 3 to 6).

## 2. Messages through RabbitMQ and MassTransit, pinned to version 8

- **Context.** Services must work together without all being up at the same time.
- **Decision.** RabbitMQ as the broker, and MassTransit for consumers, retries, the outbox, sagas and test harnesses. It is pinned to 8.5.x, because version 9 is commercial.
- **Consequences.** Less hand-written plumbing, plus a built-in test harness. The cost is a dependency on one library's conventions and a licence to check before using version 9 in paid work.

## 3. Transactional outbox instead of publishing after saving

- **Context.** A dual write lost events when a crash came between the save and the publish (Session 46).
- **Decision.** The MassTransit EF Core outbox in every service, plus a transaction and an inbox around every consumer.
- **Consequences.** No lost events, and consumers that fail halfway leave nothing behind. Delivery becomes at-least-once, so every consumer must be idempotent, and messages arrive about a second later.

## 4. Saga orchestration instead of a distributed transaction (2PC)

- **Context.** One order touches three databases.
- **Decision.** One state machine in Orders, with compensation (ReleaseStock) when payment fails.
- **Consequences.**
  - **Availability:** orders are accepted while Payments is down and finish later (Drill A). 2PC would need every service up at the same moment.
  - **Loose coupling:** each service commits locally, and no locks are held across services.
  - **The price:** eventual consistency (the order is Pending for a while) and more code to test.

## 5. Saga orchestration instead of choreography

- **Context.** With choreography, the failure rules were spread across four consumers, and nobody could see an order's progress in one place.
- **Decision.** One coordinator (`OrderStateMachine`) with its state in `order_states`.
- **Consequences.** There is one place to read and change the flow, and the explicit rules cover duplicates and late events. The saga becomes a central component, so it is tested with the harness (`OrderSagaTests`).

## 6. Optimistic concurrency on stock

- **Context.** Two orders could both reserve the last unit (Session 49).
- **Decision.** PostgreSQL `xmin` as the concurrency token on `inventory_items` (and on `order_states`).
- **Consequences.** No locks and no waiting. The loser of a race gets an exception, is retried, and is rejected with fresh data. This works because conflicts are rare. With very frequent conflicts, pessimistic locking would waste less work.

## 7. Integration tests against real PostgreSQL (Testcontainers)

- **Context.** Fakes behave differently from the real database exactly where bugs hide: SQL translation, unique indexes, migrations and `xmin`.
- **Decision.** Testcontainers starts a throwaway PostgreSQL for integration and concurrency tests. Fast unit and saga tests need no container.
- **Consequences.** The tests are trustworthy, and CI catches database problems. They are slower (seconds, not milliseconds) and need Docker locally and in CI.

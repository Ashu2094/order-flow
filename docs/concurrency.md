# Concurrency: the last unit

## The race

Reserving stock is "read, check, write": read the quantity, check that it is enough, write the new quantity. When two orders for the last unit are handled at the same time (two consumer instances, or two messages in parallel), both read 1, both decide there is enough, and both write 0. One unit was promised to two customers.

The stock number alone never shows this, because both wrote 0. The damage is two Confirmed orders for one item.

A C# `lock` cannot fix it. The two orders may be handled by different processes or machines, and a lock only works inside one process. The database must settle it.

## The fix: optimistic concurrency with xmin

PostgreSQL gives every row a hidden version, the system column `xmin`, which changes on every update. `InventoryItem` maps it as a concurrency token:

```csharp
public uint Version { get; private set; }                // InventoryItem
builder.Property(i => i.Version).IsRowVersion();         // EF configuration
```

EF Core now updates with `UPDATE ... WHERE "Id" = @id AND xmin = @versionIRead`. The first update succeeds and changes `xmin`. The second matches zero rows, and EF Core throws `DbUpdateConcurrencyException` instead of silently overwriting. The consumer's transaction rolls back, the retry policy runs it again, it reads the new quantity of 0, and it rejects the order with StockReservationFailed.

No new column was needed, because `xmin` exists in every PostgreSQL table. The migration only updated the model snapshot.

The saga's `order_states` table uses the same token, so two messages for the same order cannot overwrite each other.

## Optimistic vs pessimistic

| | Optimistic (used here) | Pessimistic |
|---|---|---|
| How | let both try, detect the conflict with a version, then retry or reject | lock the row first (`SELECT ... FOR UPDATE`), so others wait |
| Good when | conflicts are rare | conflicts are frequent and retries are expensive |
| Cost | the loser does its work twice (once wasted) | waiting, lower throughput, possible deadlocks |

Stock conflicts only happen on the last few units of a product, so optimistic is the better fit.

## Proof

- **Manual demo.** Seed `LAST-001` with quantity 1 (`inventory.http` request 23), then fire two orders at the same moment:

  ```
  for i in 1 2; do curl -s -X POST http://localhost:5214/api/orders -H "Content-Type: application/json" -d '{"customerName":"Race","items":[{"sku":"LAST-001","quantity":1}]}' & done; wait
  ```

  The result should be exactly one Confirmed, one Failed, and a stock of 0. Overlap is easier to see with a temporary 2-second delay in the consumer, added on a throwaway branch.
- **Automated test.** `StockConcurrencyTests` reads the same row in two separate contexts before either saves. Then it asserts that the second save throws `DbUpdateConcurrencyException`. It controls the interleaving instead of hoping two requests overlap, so it gives the same answer every run. If you remove `IsRowVersion`, the test fails. Run it with `dotnet test --filter "Category=Concurrency"`.

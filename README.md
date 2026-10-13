# OrderFlow
## Orders API

| Method | Route | Success | Errors |
|---|---|---|---|
| GET | `/ping` | 200 | — |
| GET | `/api/orders?page=1&pageSize=10` | 200 | 400 (bad paging) |
| GET | `/api/orders/{id}` | 200 | 404 |
| POST | `/api/orders` | 201 + Location | 400 |

Run the requests in `src/Orders/OrderFlow.Orders.Api/orders.http`.

The project-reference diagram is in `docs/project-references.md`.

### Known gaps

- Orders does not check that a SKU exists in Inventory; the client sends the unit price.
- A JSON `null` inside the items list is not validated and gives a 500.
- The total count and the page are two separate queries, so the count can be off by one if an order is added between them.
- There is no way yet to change an order's status.

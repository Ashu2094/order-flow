# Order flow

This page shows how an order moves through the system. The Orders saga (an orchestrator) decides every step, and Inventory and Payments only do their own work. Solid arrows with filled heads (`->>`) are calls that wait for an answer. Arrows with open heads (`-)`) are asynchronous messages.

## Happy path

```mermaid
sequenceDiagram
    actor Client
    participant O as Orders API
    participant S as Orders saga
    participant I as Inventory
    participant P as Payments
    Client->>O: POST /api/orders
    O->>I: GET price (HTTP)
    I-->>O: unit price
    Note over O: one transaction: order + OrderSubmitted in the outbox
    O-->>Client: 201 Pending
    O-)I: OrderSubmitted (event)
    O-)S: OrderSubmitted (event)
    Note over S: AwaitingStock
    I-)S: StockReserved (event)
    S-)P: ChargePayment (command)
    Note over S: AwaitingPayment
    P-)S: PaymentCompleted (event)
    Note over S: Confirmed
    S-)O: OrderConfirmed (event)
    Note over O: order row is Confirmed
```

## Payment fails: compensation

```mermaid
sequenceDiagram
    participant O as Orders API
    participant S as Orders saga
    participant I as Inventory
    participant P as Payments
    O-)I: OrderSubmitted (event)
    O-)S: OrderSubmitted (event)
    I-)S: StockReserved (event)
    S-)P: ChargePayment (command)
    P-)S: PaymentFailed (event)
    Note over S: Releasing
    S-)I: ReleaseStock (command)
    Note over I: stock is given back
    I-)S: StockReleased (event)
    Note over S: Cancelled
    S-)O: OrderCancelled (event)
    Note over O: order row is Cancelled
```

## Not enough stock

```mermaid
sequenceDiagram
    participant O as Orders API
    participant S as Orders saga
    participant I as Inventory
    O-)I: OrderSubmitted (event)
    O-)S: OrderSubmitted (event)
    I-)S: StockReservationFailed (event)
    Note over S: Failed
    S-)O: OrderFailed (event)
    Note over O: order row is Failed
```

## Order status (the order row)

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Confirmed: OrderConfirmed
    Pending --> Cancelled: OrderCancelled
    Pending --> Failed: OrderFailed
```

An order only leaves Pending, and it never leaves Confirmed, Cancelled or Failed. The domain methods `Confirm()`, `Cancel()` and `MarkFailed()` enforce this, and `OrderStatusTests` checks it.

The saga has more states than the order row (AwaitingStock, AwaitingPayment, Releasing). The order row shows Pending for all of them. See [order-saga.md](order-saga.md).

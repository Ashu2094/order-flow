
# Project references

```mermaid
flowchart TD
    subgraph Inventory
        IA[Inventory.Api] --> IAp[Inventory.Application]
        IA --> II[Inventory.Infrastructure]
        IAp --> ID[Inventory.Domain]
        II --> ID
    end

    subgraph Orders
        OA[Orders.Api] --> OAp[Orders.Application]
        OA --> OI[Orders.Infrastructure]
        OAp --> OD[Orders.Domain]
        OI --> OD
    end

    C[Contracts]
```

# Monolith vs microservices: what I saw

> Keep this in your own words. The lines below are a starting point based on what OrderFlow does today. Edit each one so it describes what *you* experienced.

1. **A real benefit I saw:** Inventory, Orders and Payments change and deploy independently. I rebuilt only `orders-api` many times without touching the other services.
2. **A second benefit:** failure isolation. When Payments was killed (see [drills.md](drills.md)), orders were still accepted and finished once Payments came back.
3. **One thing that got harder:** there is no shared transaction or JOIN across services. Keeping data correct took an outbox, idempotent consumers, a saga with compensation and optimistic concurrency.
4. **A second thing that got harder:** debugging and setup. One order's story is spread across three logs, three databases and a broker, and running it needs Docker Compose with eight containers.
5. **I would pick a single monolith instead when:** the team is small, the domain is still changing quickly, and nothing needs to scale or fail independently. A well-structured monolith can be split later along the same boundaries.

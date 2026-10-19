## How to run

### Prerequisites

- Docker Desktop, running
- Git

### Start everything

1. Clone the repository and open a terminal in its folder.
2. Copy the example settings file:

   **PowerShell**
   ```powershell
   Copy-Item .env.example .env
   ```

   **macOS, Linux, or Git Bash**
   ```bash
   cp .env.example .env
   ```

3. Open `.env` and set values for `ORDERS_DB_PASSWORD` and `INVENTORY_DB_PASSWORD`.
4. Start the complete system:

   ```bash
   docker compose up --build
   ```

5. Wait for both APIs to start. The first build may take a few minutes.

### Check it works

- **Inventory:** [http://localhost:5213/ping](http://localhost:5213/ping)
- **Inventory API docs:** [http://localhost:5213/scalar](http://localhost:5213/scalar)
- **Orders:** [http://localhost:5214/ping](http://localhost:5214/ping)
- **Orders API docs:** [http://localhost:5214/scalar](http://localhost:5214/scalar)

If `/scalar` does not load, try `/scalar/v1`.

### Try it

1. In `inventory.http`, send requests 20 and 21 to create two inventory items.
2. In `orders.http`, send request 02 to create an order.
3. The order prices come from the Inventory API.

### Stop

- `docker compose down` stops the services while preserving the database volumes.
- `docker compose down -v` also removes the database volumes and their stored data.

## Develop without Docker

1. Start only the databases:

   ```bash
   docker compose up -d orders-db inventory-db
   ```

2. Configure connection strings using `dotnet user-secrets set` in each API project, with keys `ConnectionStrings:InventoryDb` and `ConnectionStrings:OrdersDb`. Use `localhost` and the corresponding host-mapped database port when connecting from your computer.

3. Apply migrations using `Database__MigrateOnStartup=true` when starting each API, or use `dotnet ef database update` with the correct project and database configuration.

4. Run each API in its own terminal:

   ```bash
   dotnet watch --project src/Inventory/OrderFlow.Inventory.Api --launch-profile http
   ```

   ```bash
   dotnet watch --project src/Orders/OrderFlow.Orders.Api --launch-profile http
   ```

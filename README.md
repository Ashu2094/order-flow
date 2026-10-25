# OrderFlow

[![CI](https://github.com/Ashu2094/order-flow/actions/workflows/ci.yml/badge.svg)](https://github.com/Ashu2094/order-flow/actions/workflows/ci.yml)

OrderFlow is a small order-processing system built as three .NET services: **Orders**, **Inventory** and **Payments**. Each service owns its own PostgreSQL database, and the services talk to each other over HTTP and RabbitMQ.

## Tech stack

- .NET 10 (nullable reference types on, warnings treated as errors)
- ASP.NET Core Web APIs
- PostgreSQL 16 (one database per service)
- RabbitMQ 4 for messaging
- Docker Compose for local setup
- GitHub Actions for CI

## Services

| Service       | Host port | Database (host port)    | Notes                                              |
|---------------|-----------|-------------------------|----------------------------------------------------|
| Orders API    | `5214`    | `orders` (`5433`)       | Calls the Inventory API over HTTP                  |
| Inventory API | `5213`    | `inventory` (`5434`)    |                                                    |
| Payments API  | `5215`    | `payments` (`5435`)     | Simulated payments; failure can be forced for tests |
| RabbitMQ      | `5672`    | —                       | Management UI at http://localhost:15672            |

Each API applies its database migrations on startup (`Database__MigrateOnStartup=true`).

## Getting started

### Prerequisites

- [Docker](https://www.docker.com/) with Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/) (only needed to build or run outside Docker)

### 1. Configure environment variables

```bash
cp .env.example .env
```

Then edit `.env` and set real values:

| Variable                | Required | Purpose                                         |
|-------------------------|----------|-------------------------------------------------|
| `ORDERS_DB_PASSWORD`    | Yes      | Password for the Orders database                |
| `INVENTORY_DB_PASSWORD` | Yes      | Password for the Inventory database             |
| `PAYMENTS_DB_PASSWORD`  | Yes      | Password for the Payments database              |
| `RABBITMQ_USER`         | Yes      | RabbitMQ username                               |
| `RABBITMQ_PASSWORD`     | Yes      | RabbitMQ password                               |
| `PAYMENT_FORCE_FAILURE` | No       | Set to `true` to make every payment fail (default `false`) |

`.env` is git-ignored; never commit it.

### 2. Start everything

```bash
docker compose up --build
```

Compose waits for each database and RabbitMQ to report healthy before starting the APIs.

Quick check:

```bash
curl http://localhost:5214/ping
```

### 3. Stop

```bash
docker compose down        # stop containers, keep data
docker compose down -v     # stop containers and delete database volumes
```

## Orders API

Base URL: `http://localhost:5214`

| Method | Route                            | Success        | Errors           |
|--------|----------------------------------|----------------|------------------|
| GET    | `/ping`                          | 200            | —                |
| GET    | `/api/orders?page=1&pageSize=10` | 200            | 400 (bad paging) |
| GET    | `/api/orders/{id}`               | 200            | 404              |
| POST   | `/api/orders`                    | 201 + Location | 400              |

Ready-made requests are in `src/Orders/OrderFlow.Orders.Api/orders.http`.

## Testing payment failures

To see how the system behaves when a payment is declined, set this in `.env` and restart the Payments API:

```bash
PAYMENT_FORCE_FAILURE=true
```

```bash
docker compose up -d payments-api
```

## Running tests

```bash
dotnet tool restore
dotnet build
dotnet test
```

CI runs on every push and pull request via `.github/workflows/ci.yml`.

## Project structure

```
order-flow/
├── .github/workflows/      # CI pipeline
├── docs/                   # Design notes and diagrams
├── src/
│   ├── Orders/             # Orders API
│   ├── Inventory/          # Inventory API
│   └── Payments/           # Payments API
├── tests/                  # Automated tests
├── Directory.Build.props   # Shared build settings for all projects
├── docker-compose.yml      # Databases, RabbitMQ and the three APIs
├── .env.example            # Template for local secrets
└── order-flow.slnx         # Solution file
```

The project-reference diagram is in [`docs/project-references.md`](docs/project-references.md).

## Known gaps

- Orders does not check that a SKU exists in Inventory; the client sends the unit price.
- A JSON `null` inside the items list is not validated and gives a 500.
- The total count and the page are two separate queries, so the count can be off by one if an order is added between them.
- There is no way yet to change an order's status.
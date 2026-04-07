# 🛒 Distributed Order Processing Platform

A distributed, event-driven order processing platform built with .NET 9, 
RabbitMQ, Blazor, and React.

## 🏗️ System Architecture
[Blazor Customer Portal]     [React Admin Dashboard]
│                              │
└──────────┬───────────────────┘
│ HTTP
▼
[Order Management API]
│
▼
[RabbitMQ]
/     |      
▼      ▼       ▼
[Inventory] [Payment] [Shipping]
\      |       /
▼     ▼      ▼
[Order API updates status]

## 📦 Service Responsibilities

| Service | Responsibility |
|---|---|
| **Order Management API** | Central hub — creates orders, publishes events, tracks status |
| **Inventory Service** | Checks stock availability, confirms or fails inventory |
| **Payment Service** | Simulates payment processing with retry logic |
| **Shipping Service** | Creates shipment with tracking number |
| **Blazor Portal** | Customer UI — browse products, cart, checkout, order tracking |
| **React Admin** | Admin UI — monitor orders, view failures, filter by status |
| **Shared.Contracts** | Shared message contracts and DTOs between services |

## 🔄 Event Flow

Customer places order via Blazor
Order API creates order → publishes OrderSubmitted to RabbitMQ
Inventory Service consumes OrderSubmitted
→ Checks stock availability
→ Publishes InventoryConfirmed or InventoryFailed
Payment Service consumes InventoryConfirmed
→ Simulates payment with retry logic
→ Publishes PaymentApproved or PaymentRejected
Shipping Service consumes PaymentApproved
→ Generates tracking number
→ Publishes ShippingCreated or ShippingFailed
Order API listens to all result queues
→ Updates order status at each stage
→ Final status: Completed or Failed

## 📊 Order Status Lifecycle

Submitted → InventoryPending → InventoryConfirmed → PaymentPending
→ PaymentApproved → ShippingPending → ShippingCreated → Completed
Or on failure:
Submitted → InventoryFailed → Failed
Submitted → InventoryConfirmed → PaymentFailed → Failed
Submitted → InventoryConfirmed → PaymentApproved → ShippingFailed → Failed

## 🚀 How to Run

### Prerequisites
- Docker Desktop
- .NET 9 SDK
- Node.js 18+

### Run with Docker (Backend Services)
```bash
docker-compose up --build
```

This starts:
- RabbitMQ (ports 5672, 15672)
- Order Management API (port 5292)
- Inventory Service
- Payment Service
- Shipping Service

### Run Blazor Customer Portal
```bash
dotnet run --project BlazorPortal/BlazorPortal.csproj
```
Open: http://localhost:5017

### Run React Admin Dashboard
```bash
cd admin-dashboard
npm install
npm start
```
Open: http://localhost:3000

### Run Tests
```bash
dotnet test SportsSln.sln
```

## 🌐 Service URLs

| Service | URL |
|---|---|
| Order API Swagger | http://localhost:5292/swagger |
| RabbitMQ Dashboard | http://localhost:15672 (Sport/123) |
| Blazor Customer Portal | http://localhost:5017 |
| React Admin Dashboard | http://localhost:3000 |

## 🛠️ Tech Stack

| Technology | Usage |
|---|---|
| .NET 9 | Backend APIs and worker services |
| ASP.NET Core | Order Management REST API |
| Blazor Server | Customer portal frontend |
| React | Admin dashboard frontend |
| RabbitMQ | Async message broker |
| Entity Framework Core 9 | Database ORM |
| SQLite | Database |
| MediatR | CQRS pattern implementation |
| AutoMapper | Object mapping |
| Serilog | Structured logging |
| Docker Compose | Container orchestration |
| xUnit | Unit testing |

## 🏛️ Architecture Patterns

### CQRS with MediatR
Commands and queries are clearly separated:
- **Commands:** CheckoutOrderCommand, UpdateOrderStatusCommand
- **Queries:** GetOrderByIdQuery, GetOrdersQuery, GetCustomerOrdersQuery

Controllers are thin — all business logic lives in handlers.

### Event-Driven Architecture
Services communicate exclusively through RabbitMQ messages.
No direct service-to-service HTTP calls.

### AutoMapper
All entity-to-DTO mapping handled through MappingProfile.
Controllers never contain manual mapping code.

## 📝 Assumptions and Limitations

- Payment is simulated with 85% approval rate and one retry
- Inventory check simulates real stock with configurable limits
- Stock is reduced when an order is placed
- SQLite is used for simplicity — can be swapped for SQL Server
- Blazor runs outside Docker for development convenience
- Authentication/authorisation not implemented (out of scope)

## 🧪 Testing

26 tests covering:
- Cart operations (add, remove, calculate total)
- Order status transitions
- CQRS query handlers
- Customer order filtering
- Full order lifecycle simulation

Run with: `dotnet test SportsSln.sln`











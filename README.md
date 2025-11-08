# Ticket Management System

A comprehensive, production-ready ticket booking system built with .NET 8, implementing distributed locking, concurrency control, and observability.

## Features

- **Distributed Locking**: Redis-based distributed locks to prevent race conditions
- **Concurrency Control**: Handles 500,000+ concurrent users during flash sales
- **Microservices Architecture**: Separate services for Auth, Events, Inventory, Booking, Search, and Payment
- **Observability**: Comprehensive logging, metrics, and distributed tracing
- **Lock Contention Monitoring**: Real-time monitoring and optimization of lock contention
- **Idempotency**: Prevents duplicate bookings from retries
- **Reservation System**: Temporary seat holds with automatic expiration

## Architecture

### Services

1. **API Gateway** (Port 5000) - Single entry point with reverse proxy
2. **Auth Service** (Port 5002) - JWT-based authentication
3. **Events Service** (Port 5003) - Event management
4. **Inventory Service** (Port 5001) - Seat inventory and reservations
5. **Booking Service** (Port 5005) - Booking confirmation and payment processing
6. **Search Service** (Port 5006) - Event search (Elasticsearch integration ready)
7. **Payment Service** (Port 5004) - Payment processing

### Database Schema

- **Events DB**: Event metadata
- **Inventory DB**: Seats and reservations
- **Booking DB**: Bookings and idempotency keys
- **Auth DB**: Users and authentication

## Prerequisites

- .NET 8 SDK
- SQL Server (or SQL Server Express)
- Redis Server
- Visual Studio 2022 or VS Code

## Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ticket-management
   ```

2. **Start Redis**
   ```bash
   # Windows (using Docker)
   docker run -d -p 6379:6379 redis:latest
   
   # Or install Redis locally
   ```

3. **Update Connection Strings**
   Edit `appsettings.json` in each service to match your SQL Server and Redis configuration.

4. **Build the solution**
   ```bash
   dotnet build
   ```

5. **Run all services**
   ```bash
   # Terminal 1 - API Gateway
   cd src/TicketManagement.API.Gateway
   dotnet run

   # Terminal 2 - Auth Service
   cd src/TicketManagement.Services.Auth
   dotnet run

   # Terminal 3 - Events Service
   cd src/TicketManagement.Services.Events
   dotnet run

   # Terminal 4 - Inventory Service
   cd src/TicketManagement.Services.Inventory
   dotnet run

   # Terminal 5 - Booking Service
   cd src/TicketManagement.Services.Booking
   dotnet run

   # Terminal 6 - Payment Service
   cd src/TicketManagement.Services.Payment
   dotnet run

   # Terminal 7 - Search Service
   cd src/TicketManagement.Services.Search
   dotnet run
   ```

## Testing

### JMeter Test

1. Open JMeter
2. Load `tests/jmeter/TicketBookingTest.jmx`
3. Configure base URL and event ID
4. Run the test

### Gatling Test

1. Install Scala and sbt
2. Navigate to `tests/gatling`
3. Run:
   ```bash
   sbt gatling:test
   ```

## API Endpoints

### Public Endpoints

- `GET /api/events` - List events
- `GET /api/events/{id}` - Get event details
- `GET /api/search?q={query}` - Search events
- `GET /api/inventory/events/{eventId}/seats` - Get available seats

### Authenticated Endpoints

- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Register
- `POST /api/inventory/reservations` - Reserve seats
- `POST /api/bookings/confirm` - Confirm booking
- `GET /api/bookings/me` - Get user bookings

### Monitoring Endpoints

- `GET /api/monitoring/lock-contention` - Get lock contention stats
- `GET /api/monitoring/lock-contention/{resourceKey}` - Get stats for specific resource

## Lock Contention Monitoring

The system includes built-in lock contention monitoring:

- Tracks lock acquisition attempts and failures
- Measures average lock duration
- Calculates contention rates
- Provides real-time statistics via API

## Performance Optimization

- **Lock Timeout**: 30 seconds (configurable)
- **Reservation Timeout**: 10 minutes
- **Sorted Lock Acquisition**: Prevents deadlocks
- **Optimistic Locking**: Version-based concurrency control
- **Connection Pooling**: Efficient database connections

## Observability

- **Logging**: Serilog with file and console sinks
- **Metrics**: Custom metrics collector for business events
- **Tracing**: Distributed tracing with ActivitySource
- **Lock Monitoring**: Real-time lock contention tracking

## License

MIT License


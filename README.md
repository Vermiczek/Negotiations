# Negotiations API

Simple ASP.NET Core API for product price negotiations, using PostgreSQL and Docker.

## Prerequisites

- Docker and Docker Compose

## Quick Start

1. **Clone the repository**
```bash
git clone https://github.com/Vermiczek/Negotiations.git
cd NegotiationsApi
```

2. **Run with Docker**
```bash
# Build and start containers
docker-compose up -d

# Check status
docker-compose ps
```

3. **Access the API**
- API: http://localhost:8080/api
- Swagger UI: http://localhost:8080/swagger

## Environment Configuration

The default configuration uses:
- PostgreSQL database named `itemsdb`
- Default user: `postgres` / password: `postgres`

To customize settings, create a `.env` file in the WebApi directory:
```bash
# Example .env file
DB_HOST=postgres
DB_NAME=itemsdb
DB_USER=postgres
DB_PASSWORD=postgres
```

## Main Features

- Product management (create, view, update, delete)
- Price negotiation between clients and sellers
- User authentication and role-based authorization

## API Basics

### Products
- GET `/api/products` - View all products
- POST `/api/products` - Create product (auth required)

### Negotiations
- POST `/api/negotiations` - Create price negotiation request
- GET `/api/negotiations/client` - View your negotiations

### Authentication
- POST `/api/auth/register` - Register new user
- POST `/api/auth/login` - Login to get JWT token

## Stopping the Application

```bash
# Stop containers but keep data
docker-compose down

# Stop and remove all data
docker-compose down -v
```

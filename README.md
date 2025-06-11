# ASP.NET 9/10 WebAPI with PostgreSQL in Docker

This is a simple WebAPI project using ASP.NET with PostgreSQL database, containerized with Docker.

## Features

- ASP.NET Web API
- PostgreSQL database integration with Entity Framework Core
- Docker and Docker Compose setup
- RESTful API endpoints for CRUD operations
- Automatic database migrations

## Prerequisites

- Docker and Docker Compose
- .NET SDK (for local development)

## Getting Started

1. Clone this repository
2. Navigate to the project directory
3. Run Docker Compose:

```bash
docker-compose up -d
```

The API will be available at http://localhost:8080/api/items

## API Endpoints

- **GET /api/items** - Get all items
- **GET /api/items/{id}** - Get a specific item by ID
- **POST /api/items** - Create a new item
- **PUT /api/items/{id}** - Update an existing item
- **DELETE /api/items/{id}** - Delete an item

## Sample Item JSON

```json
{
  "name": "Sample Item",
  "description": "This is a sample item description"
}
```

## Local Development

To run the application locally:

```bash
cd WebApi
dotnet run
```

## Technologies

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker

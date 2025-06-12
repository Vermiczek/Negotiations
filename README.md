# ASP.NET 9/10 WebAPI with PostgreSQL in Docker

This is a WebAPI project using ASP.NET with PostgreSQL database, containerized with Docker, implementing a price negotiation process for products.

## Features

- ASP.NET Web API with RESTful endpoints
- PostgreSQL database integration with Entity Framework Core
- Docker and Docker Compose setup
- Role-based authentication and authorization
- Price negotiation process for products
- Automatic database migrations

## Docker Setup

### Environment Configuration

The application uses a `.env` file for environment configuration:

- Located in the `WebApi/` directory
- Contains database connection settings, JWT configuration, and other environment variables
- Used by both the application and Docker Compose

To set up your environment:

1. Copy the example configuration file:
   ```bash
   cp WebApi/.env.example WebApi/.env
   ```

2. Edit the `.env` file to customize your settings:
   ```bash
   # Database connection
   DB_HOST=postgres
   DB_NAME=itemsdb
   DB_USER=postgres
   DB_PASSWORD=postgres
   
   # JWT Authentication
   JWT_SECRET=yourSecretKeyHere
   JWT_ISSUER=NegotiationsApi
   JWT_AUDIENCE=NegotiationsClient
   JWT_EXPIRY_MINUTES=120
   
   # Server configuration
   ASPNETCORE_URLS=http://+:8080
   ASPNETCORE_ENVIRONMENT=Development
   
   # Logging
   LOGGING_LEVEL_DEFAULT=Information
   LOGGING_LEVEL_MICROSOFT=Warning
   ```

> Note: When running the setup.sh script, a `.env` file will be created automatically if one doesn't exist.

### Health Checks

This application includes integrated health checks for both the ASP.NET application and PostgreSQL database:

- **API Health Check**: Available at http://localhost:8080/health
  - Provides information about the API and database connection status
  - Returns HTTP 200 when healthy, with detailed component statuses

- **PostgreSQL Health Check**: Used by Docker Compose to ensure database readiness
  - Uses `pg_isready` to check PostgreSQL availability
  - API container waits for positive health status before starting

These health checks ensure the services start in the correct order and provide easy monitoring of application health.

### Quick Start

1. **Clone the repository**:
```bash
git clone <repository-url>
cd NegotiationsApi
```

2. **Set up environment configuration**:
```bash
cp WebApi/.env.example WebApi/.env
# Optional: Edit WebApi/.env to customize settings
```

3. **Build and run the containers**:
```bash
docker-compose up -d --build
```

   Alternatively, you can use the setup script which will create the .env file automatically:
```bash
./setup.sh
```

3. **Check container status**:
```bash
docker-compose ps
```

4. **View logs**:
```bash
docker-compose logs -f
```

5. **Access the API**:
   - API endpoint: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger

### Persistence

The PostgreSQL data is persisted in a Docker volume (`postgres_data`). This means your data will survive container restarts.

### Stopping and Cleaning Up

- **Stop containers**:
```bash
docker-compose down
```

- **Stop containers and remove volumes (clears database)**:
```bash
docker-compose down -v
```

### Troubleshooting

If you encounter issues:

1. **Check logs**:
```bash
docker-compose logs -f webapi
docker-compose logs -f postgres
```

2. **Rebuild from scratch**:
```bash
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

3. **Connect to PostgreSQL directly**:
```bash
docker exec -it postgres psql -U postgres -d itemsdb
```

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

The API will be available at http://localhost:8080/api

## API Endpoints

### Products

- **GET /api/products** - Get all products
- **GET /api/products/{id}** - Get a specific product by ID
- **POST /api/products** - Create a new product (requires seller or admin role)
- **PUT /api/products/{id}** - Update an existing product (requires seller or admin role)
- **DELETE /api/products/{id}** - Delete a product (requires admin role)

### Negotiations

- **GET /api/negotiations** - Get all negotiations (requires seller or admin role)
- **GET /api/negotiations/{id}** - Get a specific negotiation (using Client-Identifier header or email query parameter)
- **GET /api/negotiations/client** - Get all negotiations for the current client (using Client-Identifier header or email query parameter)
- **GET /api/negotiations/product/{productId}** - Get negotiations for a specific product (requires seller or admin role)
- **POST /api/negotiations** - Create a new price negotiation (requires clientEmail in request body, Client-Identifier header optional)
- **POST /api/negotiations/{id}/respond** - Respond to a negotiation (accept/reject) (requires seller or admin role)
- **POST /api/negotiations/{id}/propose-new-price** - Propose a new price after rejection (using Client-Identifier header or email query parameter)

### Seller Dashboard

- **GET /api/seller/dashboard** - Get seller dashboard data (requires seller role)
- **GET /api/seller/listings** - Get seller listings (requires seller role)
- **POST /api/seller/listings** - Create a new listing (requires seller role)

## Sample JSON Formats

### Product

```json
{
  "name": "Sample Product",
  "description": "This is a sample product description",
  "price": 99.99
}
```

### Negotiation Create

```json
{
  "productId": 1,
  "proposedPrice": 75.99,
  "clientEmail": "client@example.com",
  "clientName": "John Doe"
}
```

### Negotiation Response

```json
{
  "isAccepted": true,
  "comment": "We accept your offer."
}
```
Or
```json
{
  "isAccepted": false,
  "comment": "This price is too low, please consider a higher offer."
}
```

### New Price Proposal

```json
{
  "proposedPrice": 85.99
}
```

## Client Identification

The API supports two methods of client identification:

1. **Client Identifier Header** (legacy): Send a `Client-Identifier` header with each request:
   ```
   Client-Identifier: client-12345
   ```

2. **Email-based Identification** (recommended): Use the client's email address
   - When creating a negotiation: Include `ClientEmail` in the request body
   - When accessing a negotiation: Include `email` as a query parameter:
     ```
     GET /api/negotiations/123?email=client@example.com
     ```

For backward compatibility, both identification methods are supported. However, the email-based approach is recommended as it:
- Provides better traceability and communication options
- Is more user-friendly and memorable than random identifiers
- Supports validation through the `[EmailAddress]` attribute

## Authentication

The API uses JWT authentication for seller and admin users. To authenticate:

1. Get a token using the `/api/auth/login` endpoint
2. Include the token in the Authorization header for protected endpoints:
   ```
   Authorization: Bearer {your-jwt-token}
   ```

## Negotiation Rules

- Each client can make up to 3 price offers per product
- After rejection, clients have 7 days to make a new proposal
- Only authenticated employees (with role "seller") can respond to negotiations

## Local Development

To run the application locally:

1. **Set up environment variables**:
```bash
cp WebApi/.env.example WebApi/.env
# Edit WebApi/.env as needed
```

2. **Run the application**:
```bash
cd WebApi
dotnet run
```

The application will load environment variables from the `.env` file. You can override any settings by setting environment variables directly:

```bash
# Example: Override database settings
export DB_HOST=localhost
export DB_NAME=devdb
cd WebApi
dotnet run
```

## Technologies

- ASP.NET Core
- Entity Framework Core
- JWT Authentication
- PostgreSQL
- Docker
- Swagger/OpenAPI

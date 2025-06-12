# Negotiations API Integration Tests

This project contains integration tests for the Negotiations API. The tests are designed to validate core functionality of the API, particularly the email-based client identification feature.

## Test Structure

The integration tests are organized into several main categories:

1. **Authentication Tests** - Tests login functionality for both admin and seller roles
2. **Product Management Tests** - Tests CRUD operations for products
3. **Negotiations Tests** - Tests negotiation creation, status changes, and access controls
4. **Email Identification Tests** - Specific tests for the email-based client identification feature

## Prerequisites

- .NET SDK 9.0 or higher
- Running instance of the Negotiations API (either in Docker or locally)
- PostgreSQL database configured for testing

## Configuration

The tests use an `appsettings.json` file with test-specific configuration. Make sure the database connection string is correctly set for your test environment.

## Running the Tests

### Running all tests

```bash
cd /Users/babizak/GoProjects/Negotiations
dotnet test NegotiationsApi.IntegrationTests
```

### Running specific test categories

```bash
dotnet test NegotiationsApi.IntegrationTests --filter "FullyQualifiedName~EmailIdentification"
dotnet test NegotiationsApi.IntegrationTests --filter "FullyQualifiedName~Authentication"
```

## Test Against Production API

To run tests against a production API:

1. Update the `appsettings.json` file to point to your production database
2. Set environment variables to override configuration if needed:

```bash
export ConnectionStrings__DefaultConnection="Host=production-db;Database=negotiationsdb;Username=username;Password=password"
dotnet test NegotiationsApi.IntegrationTests
```

## Test Features

### Email-Based Client Identification Tests

Tests validate that:
- Clients can create negotiations with email-only identification
- Clients can access their negotiations using email query parameter
- Email-based identification works alongside the legacy Client-Identifier header
- Proper authorization is enforced for all client operations

### Negotiation Flow Tests

Tests validate the complete negotiation flow:
1. Creation of a negotiation by a client
2. Response (accept/reject) by a seller
3. New price proposal by the client after rejection
4. Final resolution of the negotiation

### Authentication Tests

Tests validate:
- Proper JWT token issuance
- Role-based authorization
- Access control for admin and seller operations

# Integration Tests

This directory contains basic integration tests for the Negotiations API.

## Quick Start

Run all tests:

```bash
cd NegotiationsApi
./IntegrationTests/Scripts/run-integration-tests.sh
```

Or manually:

```bash
cd NegotiationsApi
dotnet test IntegrationTests
```

## Test Categories

- **Authentication** - Login and JWT verification
- **Products** - Create, read, update, delete tests
- **Negotiations** - Price negotiation flow tests
- **Email Identification** - Client access via email

## Configuration

Edit `IntegrationTests/appsettings.json` to configure test database connection.

## Running Specific Tests

```bash
# Run only authentication tests
dotnet test IntegrationTests --filter "FullyQualifiedName~Authentication"

# Run only negotiation tests
dotnet test IntegrationTests --filter "FullyQualifiedName~Negotiations"
```

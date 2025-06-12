# Integration Tests for Negotiations API

## Implemented Features

### Test Infrastructure
- Created `NegotiationsApiFactory` for WebApplicationFactory-based tests
- Created `TestFixture` for common testing functionality
- Added HTTP client extension methods for easier API interaction
- Created configuration-based test setup to target different environments
- Added appsettings.json with test configuration

### Authentication Tests
- Testing login functionality for both admin and seller roles
- Validating token generation and role-based access

### Product Management Tests
- Testing CRUD operations for products
- Validating admin and seller role permissions

### Negotiation Tests
- Testing negotiation creation, proposal, and response flow
- Testing client authentication using both header and email methods
- Validating email-based client identification

### Email Identification Tests
- Created dedicated tests for the new email-based client identification feature
- Testing dual identification methods (Client-Identifier header and email query parameter)
- Validating proper authorization with both methods

### Utility Scripts
- Added a `run-integration-tests.sh` script to run tests against different environments

## How to Use

### Running Tests Locally
```bash
# Run all tests
./Scripts/run-integration-tests.sh local

# Run specific test categories
./Scripts/run-integration-tests.sh local EmailIdentification
./Scripts/run-integration-tests.sh local Authentication
```

### Running Tests Against Production
```bash
# Update production connection details in the script first
./Scripts/run-integration-tests.sh production
```

## Test Coverage

The integration tests cover:

1. **Email-based client identification**
   - Creating negotiations with email-only authentication
   - Accessing negotiations using email query parameter
   - Using both identification methods together

2. **Negotiation Flow**
   - Full negotiation lifecycle from creation to resolution
   - Multiple negotiation attempts
   - Price proposal after rejection

3. **Security**
   - Authentication with JWT tokens
   - Role-based access control
   - Client identification and authorization

4. **API Health**
   - Health endpoint verification
   - Swagger documentation availability

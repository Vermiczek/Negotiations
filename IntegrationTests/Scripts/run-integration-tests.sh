#!/bin/bash

ENVIRONMENT=${1:-local}
TEST_FILTER=${2:-""}

# Configuration for different environments
case "$ENVIRONMENT" in
  local)
    CONNECTION_STRING="Host=localhost;Database=negotiationsdb;Username=postgres;Password=postgres"
    API_URL="http://localhost:8080"
    
    # Check if local API is running
    echo "Checking if local API is running..."
    if ! curl -s --head $API_URL/health | grep "200 OK" > /dev/null; then
      echo "Local API is not running. Starting it with Docker..."
      
      # Check if docker-compose exists in parent directory
      if [ -f "../docker-compose.yml" ]; then
        (cd .. && docker-compose up -d)
        echo "Waiting for API to start..."
        sleep 10
      else
        echo "Error: docker-compose.yml not found. Please start the API manually."
        exit 1
      fi
    else
      echo "Local API is running."
    fi
    ;;
  staging)
    CONNECTION_STRING="Host=staging-db;Database=negotiationsdb;Username=staging_user;Password=staging_pass"
    API_URL="https://staging-negotiations-api.example.com"
    ;;
  production)
    CONNECTION_STRING="Host=production-db;Database=negotiationsdb;Username=prod_user;Password=prod_pass"
    API_URL="https://negotiations-api.example.com"
    ;;
  *)
    echo "Unknown environment: $ENVIRONMENT"
    echo "Valid options: local, staging, production"
    exit 1
    ;;
esac

echo "Running integration tests against $ENVIRONMENT environment"
echo "API URL: $API_URL"

# Set environment variables for the test run
export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"
export ApiSettings__BaseUrl="$API_URL"

# Build the project first
echo "Building project..."
dotnet build

# Run the tests with optional filter
if [ -z "$TEST_FILTER" ]; then
  echo "Running all tests..."
  dotnet test
else
  echo "Running tests matching: $TEST_FILTER"
  dotnet test --filter "FullyQualifiedName~$TEST_FILTER"
fi

# Reset environment variables
unset ConnectionStrings__DefaultConnection
unset ApiSettings__BaseUrl

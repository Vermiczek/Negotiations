#!/bin/bash
set -e

echo "Waiting for PostgreSQL to be ready..."
# Wait for PostgreSQL to be ready
for i in {1..30}; do
  if PGPASSWORD=$POSTGRES_PASSWORD psql -h "postgres" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c '\q' 2>/dev/null; then
    echo "PostgreSQL is up - executing migrations"
    break
  fi
  echo "PostgreSQL is unavailable - sleeping (attempt $i/30)"
  sleep 2
  if [ $i -eq 30 ]; then
    echo "PostgreSQL did not become available in time. Exiting."
    exit 1
  fi
done

cd /app

# Create database if it doesn't exist
PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -tc "SELECT 1 FROM pg_database WHERE datname = '$POSTGRES_DB'" | grep -q 1 || PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -c "CREATE DATABASE $POSTGRES_DB"

echo "Migrations will be applied programmatically at application startup..."

# Start the application
echo "Starting application..."
dotnet WebApi.dll

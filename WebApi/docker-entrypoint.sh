#!/bin/bash
set -e

echo "Waiting for PostgreSQL to be ready..."
# Wait for PostgreSQL to be ready with increased timeout
for i in {1..60}; do
  if PGPASSWORD=$POSTGRES_PASSWORD psql -h "postgres" -U "$POSTGRES_USER" -d "postgres" -c '\q' 2>/dev/null; then
    echo "PostgreSQL is up - proceeding with setup"
    break
  fi
  echo "PostgreSQL is unavailable - sleeping (attempt $i/60)"
  sleep 2
  if [ $i -eq 60 ]; then
    echo "PostgreSQL did not become available in time. Exiting."
    exit 1
  fi
done

cd /app

echo "Creating database if it doesn't exist..."
# Create database if it doesn't exist - connect to default postgres database first
PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$POSTGRES_DB'" | grep -q 1 || PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -d postgres -c "CREATE DATABASE $POSTGRES_DB"

# Verify the database exists and is accessible
if ! PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -d $POSTGRES_DB -c '\q' 2>/dev/null; then
  echo "ERROR: Cannot connect to created database $POSTGRES_DB. Exiting."
  exit 1
fi

echo "Database setup successful. Migrations will be applied programmatically at application startup..."

# Start the application
echo "Starting application..."
dotnet Negotiations.dll

#!/bin/bash
set -e

# Load environment variables from .env file if it exists
if [ -f /app/.env ]; then
    echo "Loading environment variables from .env file..."
    export $(grep -v '^#' /app/.env | xargs)
fi

# Set default PostgreSQL connection parameters if not provided in environment
: ${DB_HOST:="postgres"}
: ${DB_USER:="postgres"}
: ${DB_PASSWORD:="postgres"}
: ${DB_NAME:="itemsdb"}

echo "Waiting for PostgreSQL to be ready..."
echo "Trying to connect to PostgreSQL at $DB_HOST as user $DB_USER"
for i in {1..90}; do
  if PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d "postgres" -c '\q' 2>/dev/null; then
    echo "PostgreSQL up"
    break
  fi
  echo "PostgreSQL is unavailable - sleeping (attempt $i/90)"
  sleep 2
  if [ $i -eq 90 ]; then
    echo "PostgreSQL did not become available in time. Will continue anyway and let the application retry."
  fi
done

cd /app

echo "Creating database if it doesn't exist..."
for i in {1..10}; do
  if PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'" 2>/dev/null | grep -q 1; then
    echo "Database $DB_NAME exists"
    break
  else
    echo "Creating database $DB_NAME (attempt $i/10)"
    PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d postgres -c "CREATE DATABASE $DB_NAME" 2>/dev/null || true
    sleep 2
  fi
  
  if [ $i -eq 10 ]; then
    echo "Warning: Could not verify database creation. Will continue anyway and let the application retry."
  fi
done

echo "Database setup complete. Starting application..."
echo "Application will retry connecting to database if needed."

dotnet Negotiations.dll

#!/bin/bash
set -e

# for your own sanity dont look in there - layers of fixes and workarounds, some 
echo "Waiting for PostgreSQL to be ready..."
for i in {1..90}; do
  if PGPASSWORD=$POSTGRES_PASSWORD psql -h "postgres" -U "$POSTGRES_USER" -d "postgres" -c '\q' 2>/dev/null; then
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
  if PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$POSTGRES_DB'" 2>/dev/null | grep -q 1; then
    echo "Database $POSTGRES_DB exists"
    break
  else
    echo "Creating database $POSTGRES_DB (attempt $i/10)"
    PGPASSWORD=$POSTGRES_PASSWORD psql -h postgres -U $POSTGRES_USER -d postgres -c "CREATE DATABASE $POSTGRES_DB" 2>/dev/null || true
    sleep 2
  fi
  
  if [ $i -eq 10 ]; then
    echo "Warning: Could not verify database creation. Will continue anyway and let the application retry."
  fi
done

echo "Database setup complete. Starting application..."
echo "Application will retry connecting to database if needed."

dotnet Negotiations.dll

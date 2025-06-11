#!/bin/bash
set -e

# Function to check if database exists
database_exists() {
    psql -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$POSTGRES_DB'" | grep -q 1
}

# Create database if it doesn't exist
if ! database_exists; then
    echo "Creating database $POSTGRES_DB"
    psql -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE $POSTGRES_DB"
else
    echo "Database $POSTGRES_DB already exists"
fi

# Grant all privileges to the postgres user
echo "Granting privileges on $POSTGRES_DB to $POSTGRES_USER"
psql -U "$POSTGRES_USER" -d postgres -c "GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO $POSTGRES_USER"

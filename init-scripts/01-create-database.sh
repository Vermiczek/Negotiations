#!/bin/bash
set -e

database_exists() {
    psql -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$POSTGRES_DB'" | grep -q 1
}

if ! database_exists; then
    echo "Creating database $POSTGRES_DB"
    psql -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE $POSTGRES_DB"
else
    echo "Database $POSTGRES_DB already exists"
fi

echo "Granting privileges on $POSTGRES_DB to $POSTGRES_USER"
psql -U "$POSTGRES_USER" -d postgres -c "GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO $POSTGRES_USER"

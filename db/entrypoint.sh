#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
sql_pid=$!

echo "Waiting for SQL Server to accept connections..."
until sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1
do
  sleep 2
done

echo "Running init.sql..."
sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /docker-entrypoint-initdb.d/init.sql

echo "Database initialized."
wait $sql_pid
#!/usr/bin/env bash
set -euo pipefail

sqlcmd_bin="/opt/mssql-tools18/bin/sqlcmd"
server="sqlserver"
password="${SQLCMDPASSWORD:-}"

if [ -z "$password" ]; then
  echo "SQLCMDPASSWORD is required."
  exit 1
fi

until "$sqlcmd_bin" -S "$server" -U sa -P "$password" -C -Q "SELECT 1" >/dev/null 2>&1; do
  echo "A aguardar o SQL Server..."
  sleep 2
done

echo "SQL Server aceita ligacoes. A preparar bases de dados..."

"$sqlcmd_bin" -b -S "$server" -U sa -P "$password" -C -d master -Q "
IF DB_ID('ResultadosFutebol') IS NULL CREATE DATABASE ResultadosFutebol;
IF DB_ID('Pagamentos') IS NULL CREATE DATABASE Pagamentos;
IF DB_ID('Apostas') IS NULL CREATE DATABASE Apostas;
"

for database in ResultadosFutebol Pagamentos Apostas; do
  until "$sqlcmd_bin" -S "$server" -U sa -P "$password" -C -d master -Q "SELECT state_desc FROM sys.databases WHERE name = '$database' AND state_desc = 'ONLINE'" -h -1 -W | grep -q "ONLINE" \
    && "$sqlcmd_bin" -S "$server" -U sa -P "$password" -C -d "$database" -Q "SELECT 1" >/dev/null 2>&1; do
    echo "A aguardar base de dados $database online..."
    sleep 2
  done
done

echo "Bases de dados online. A executar scripts..."

cd /work/database

attempt=1
max_attempts=6
until "$sqlcmd_bin" -b -S "$server" -U sa -P "$password" -C -i "00_Execucao_Ordem.sql"; do
  if [ "$attempt" -ge "$max_attempts" ]; then
    echo "Falha ao preparar bases de dados apos $attempt tentativas."
    exit 1
  fi

  echo "Bootstrap SQL falhou na tentativa $attempt. A repetir apos estabilizacao..."
  attempt=$((attempt + 1))
  sleep 5
done

echo "Bases de dados e objetos preparados."

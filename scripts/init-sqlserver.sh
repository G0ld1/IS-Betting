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
done

cd /work/database
"$sqlcmd_bin" -S "$server" -U sa -P "$password" -C -i "00_Execucao_Ordem.sql"
echo "Bases de dados e objetos preparados."
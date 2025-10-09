#!/bin/bash

# MySQL Database Restore Script for Insurance Claims API
# Usage: ./restore-database.sh <backup-file>

if [ -z "$1" ]; then
    echo "Usage: $0 <backup-file.sql or backup-file.sql.gz>"
    echo "Example: $0 backups/InsuranceClaimsDB_backup_20251006_120000.sql.gz"
    exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Backup file not found: $BACKUP_FILE"
    exit 1
fi

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '#' | awk '/=/ {print $1}')
fi

MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD:-root_password_change_me}
MYSQL_DATABASE=${MYSQL_DATABASE:-InsuranceClaimsDB}

echo "⚠️  WARNING: This will restore the database and overwrite existing data!"
read -p "Are you sure you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Restore cancelled."
    exit 0
fi

echo "Starting database restore..."

# Check if file is compressed
if [[ "$BACKUP_FILE" == *.gz ]]; then
    echo "Decompressing backup file..."
    gunzip -c "$BACKUP_FILE" | docker exec -i insurance-claims-mysql mysql -u root -p$MYSQL_ROOT_PASSWORD
else
    docker exec -i insurance-claims-mysql mysql -u root -p$MYSQL_ROOT_PASSWORD < "$BACKUP_FILE"
fi

if [ $? -eq 0 ]; then
    echo "✅ Database restored successfully!"
else
    echo "❌ Restore failed!"
    exit 1
fi


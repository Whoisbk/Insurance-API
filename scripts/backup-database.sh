#!/bin/bash

# MySQL Database Backup Script for Insurance Claims API
# This script creates a backup of the MySQL database

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '#' | awk '/=/ {print $1}')
fi

# Set default values if not in .env
MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD:-root_password_change_me}
MYSQL_DATABASE=${MYSQL_DATABASE:-InsuranceClaimsDB}

# Backup directory
BACKUP_DIR="./backups"
mkdir -p $BACKUP_DIR

# Backup filename with timestamp
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/${MYSQL_DATABASE}_backup_$TIMESTAMP.sql"

echo "Starting database backup..."
echo "Database: $MYSQL_DATABASE"
echo "Backup file: $BACKUP_FILE"

# Create backup using Docker
docker exec insurance-claims-mysql mysqldump \
    -u root \
    -p$MYSQL_ROOT_PASSWORD \
    --single-transaction \
    --routines \
    --triggers \
    --databases $MYSQL_DATABASE > "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo "✅ Backup completed successfully!"
    echo "Backup saved to: $BACKUP_FILE"
    
    # Compress the backup
    gzip "$BACKUP_FILE"
    echo "✅ Backup compressed: ${BACKUP_FILE}.gz"
    
    # Keep only last 7 backups
    ls -tp $BACKUP_DIR/*.gz | grep -v '/$' | tail -n +8 | xargs -I {} rm -- {}
    echo "✅ Old backups cleaned up (keeping last 7)"
else
    echo "❌ Backup failed!"
    exit 1
fi


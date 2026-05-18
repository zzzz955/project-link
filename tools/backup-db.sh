#!/bin/sh
# MySQL dump backup for the project-link stack.
# Usage: sh tools/backup-db.sh
# Recommended: add to cron -> 0 3 * * * /opt/madalang/project-link/tools/backup-db.sh
#
# Requires: docker, .env.prod in the project root
# Backup dir: /var/backups/madalang (created if missing)
# Retention: 30 days

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/madalang}"
KEEP_DAYS="${KEEP_DAYS:-30}"

ENV_FILE="$PROJECT_ROOT/.env.prod"
if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: .env.prod not found at $ENV_FILE" >&2
    exit 1
fi

CONTAINER_NAME="$(grep '^COMPOSE_PROJECT_NAME=' "$ENV_FILE" | cut -d= -f2 | tr -d '[:space:]')-mysql"
DB_NAME="$(grep '^DB_NAME=' "$ENV_FILE" | cut -d= -f2 | tr -d '[:space:]')"
DB_ROOT_PASSWORD="$(grep '^DB_ROOT_PASSWORD=' "$ENV_FILE" | cut -d= -f2 | tr -d '[:space:]')"

DATE="$(date +%F)"
mkdir -p "$BACKUP_DIR"

OUTFILE="$BACKUP_DIR/projectlink_${DATE}.sql.gz"

echo "==> Backing up project-link DB to $OUTFILE ..."
docker exec "$CONTAINER_NAME" \
    mysqldump -u root -p"$DB_ROOT_PASSWORD" --single-transaction --routines "$DB_NAME" \
    | gzip > "$OUTFILE"

echo "==> Removing backups older than $KEEP_DAYS days ..."
find "$BACKUP_DIR" -name "projectlink_*.sql.gz" -mtime +"$KEEP_DAYS" -delete

echo "==> Done: $OUTFILE"

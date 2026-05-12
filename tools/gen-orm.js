'use strict';
/**
 * gen-orm: server/db/schema.json → DB CREATE/ALTER TABLE (+ migration SQL file)
 *
 * schema.json format:
 * {
 *   "database": "game_db",
 *   "tables": [
 *     {
 *       "name": "players",
 *       "comment": "...",
 *       "columns": [
 *         { "name": "id", "type": "int64", "constraints": ["PK","AUTO","NN"] }
 *       ],
 *       "indexes": [
 *         { "name": "idx_players_email", "columns": ["email"], "unique": true }
 *       ]
 *     }
 *   ]
 * }
 */

const fs   = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');
const cfg  = require('./config-loader');

// ── SQL type maps per DB ───────────────────────────────────────────────────────
const SQL_TYPES = {
  postgresql: {
    int8: 'SMALLINT', int16: 'SMALLINT', int32: 'INTEGER',  int64: 'BIGINT',
    uint8: 'SMALLINT', uint16: 'INTEGER', uint32: 'BIGINT', uint64: 'NUMERIC(20,0)',
    float: 'REAL', double: 'DOUBLE PRECISION',
    bool: 'BOOLEAN', string: 'TEXT', datetime: 'TIMESTAMPTZ',
    date: 'DATE', json: 'JSONB', bytes: 'BYTEA',
  },
  mysql: {
    int8: 'TINYINT', int16: 'SMALLINT', int32: 'INT',    int64: 'BIGINT',
    uint8: 'TINYINT UNSIGNED', uint16: 'SMALLINT UNSIGNED',
    uint32: 'INT UNSIGNED',    uint64: 'BIGINT UNSIGNED',
    float: 'FLOAT', double: 'DOUBLE',
    bool: 'TINYINT(1)', string: 'TEXT', datetime: 'DATETIME',
    date: 'DATE', json: 'JSON', bytes: 'BLOB',
  },
  sqlite: {
    int8: 'INTEGER', int16: 'INTEGER', int32: 'INTEGER', int64: 'INTEGER',
    uint8: 'INTEGER', uint16: 'INTEGER', uint32: 'INTEGER', uint64: 'INTEGER',
    float: 'REAL', double: 'REAL',
    bool: 'INTEGER', string: 'TEXT', datetime: 'TEXT',
    date: 'TEXT', json: 'TEXT', bytes: 'BLOB',
  },
};

function resolveType(rawType, dbType) {
  const map = SQL_TYPES[dbType] || SQL_TYPES.postgresql;
  const strN = rawType.match(/^string\((\d+)\)$/);
  if (strN) {
    return dbType === 'sqlite' ? 'TEXT' : `VARCHAR(${strN[1]})`;
  }
  return map[rawType] || rawType.toUpperCase();
}

// ── Validation ─────────────────────────────────────────────────────────────────
const VALID_CONSTRAINTS = new Set(['PK','FK','NN','UQ','IDX','AUTO']);
const VALID_TYPES = new Set([
  'int8','int16','int32','int64','uint8','uint16','uint32','uint64',
  'float','double','bool','string','datetime','date','json','bytes',
]);

function isValidType(t) {
  if (VALID_TYPES.has(t)) return true;
  if (/^string\(\d+\)$/.test(t)) return true;
  return false;
}

function validateSchema(schema, filePath) {
  const rel    = path.relative(cfg.root, filePath);
  const errors = [];

  if (!schema.database) errors.push({ file: rel, loc: 'root', msg: 'Missing "database" field' });
  if (!Array.isArray(schema.tables) || schema.tables.length === 0) {
    errors.push({ file: rel, loc: 'root', msg: 'Missing or empty "tables" array' });
    return errors;
  }

  const tableNames = new Set();
  for (const tbl of schema.tables) {
    const tloc = `Table "${tbl.name ?? '(unnamed)'}"`;
    if (!tbl.name) { errors.push({ file: rel, loc: tloc, msg: 'Missing table name' }); continue; }
    if (tableNames.has(tbl.name)) {
      errors.push({ file: rel, loc: tloc, msg: `Duplicate table name "${tbl.name}"` });
    }
    tableNames.add(tbl.name);

    if (!Array.isArray(tbl.columns) || tbl.columns.length === 0) {
      errors.push({ file: rel, loc: tloc, msg: 'Table must have at least one column' });
      continue;
    }

    const colNames = new Set();
    let hasPK = false;
    for (const col of tbl.columns) {
      const cloc = `${tloc}, Column "${col.name ?? '(unnamed)'}"`;
      if (!col.name) { errors.push({ file: rel, loc: cloc, msg: 'Missing column name' }); continue; }
      if (colNames.has(col.name)) {
        errors.push({ file: rel, loc: cloc, msg: `Duplicate column name "${col.name}"` });
      }
      colNames.add(col.name);

      if (!isValidType(col.type)) {
        errors.push({ file: rel, loc: cloc,
          msg: `Invalid type "${col.type}". Valid: int8~64, uint8~64, float, double, bool, string, string(N), datetime, date, json, bytes` });
      }

      const cons = Array.isArray(col.constraints) ? col.constraints : [];
      for (const c of cons) {
        const base = c.split(':')[0];
        if (!VALID_CONSTRAINTS.has(base)) {
          errors.push({ file: rel, loc: cloc, msg: `Invalid constraint "${c}"` });
        }
        if (base === 'PK') hasPK = true;
      }

      // PK cannot be nullable
      if (cons.includes('PK') && !cons.includes('NN')) {
        errors.push({ file: rel, loc: cloc, msg: 'Primary key column must include NN constraint' });
      }
    }

    // FK reference validation
    for (const col of tbl.columns) {
      const cons = Array.isArray(col.constraints) ? col.constraints : [];
      for (const c of cons) {
        if (c.startsWith('FK:')) {
          const refTable = c.split(':')[1];
          if (!tableNames.has(refTable) && !schema.tables.find(t => t.name === refTable)) {
            errors.push({ file: rel, loc: `${tloc}, Column "${col.name}"`,
              msg: `FK references unknown table "${refTable}"` });
          }
        }
      }
    }
  }

  return errors;
}

// ── SQL generation ─────────────────────────────────────────────────────────────
function buildColumnSQL(col, dbType) {
  const cons    = col.constraints || [];
  const sqlType = resolveType(col.type, dbType);
  const parts   = [col.name, sqlType];

  if (dbType === 'mysql' && cons.includes('AUTO')) parts.push('AUTO_INCREMENT');
  if (cons.includes('NN') || cons.includes('PK')) parts.push('NOT NULL');
  if (cons.includes('UQ') && !cons.includes('PK')) parts.push('UNIQUE');
  if (col.default !== undefined) parts.push(`DEFAULT ${col.default}`);

  return parts.join(' ');
}

function generateCreateTable(tbl, dbType) {
  const lines  = [`CREATE TABLE IF NOT EXISTS ${tbl.name} (`];
  const colDefs = tbl.columns.map(col => '  ' + buildColumnSQL(col, dbType));

  const pkCols = tbl.columns.filter(c => (c.constraints || []).includes('PK')).map(c => c.name);
  if (pkCols.length) colDefs.push(`  PRIMARY KEY (${pkCols.join(', ')})`);

  if (dbType !== 'sqlite') {
    for (const col of tbl.columns) {
      const fkCon = (col.constraints || []).find(c => c.startsWith('FK:'));
      if (fkCon) {
        const ref = fkCon.split(':')[1];
        colDefs.push(`  FOREIGN KEY (${col.name}) REFERENCES ${ref}(id)`);
      }
    }
  }

  lines.push(colDefs.join(',\n'));
  if (tbl.comment && dbType === 'mysql') {
    lines.push(`) COMMENT='${tbl.comment.replace(/'/g, "\\'")}';`);
  } else {
    lines.push(');');
    if (tbl.comment && dbType === 'postgresql') {
      lines.push(`COMMENT ON TABLE ${tbl.name} IS '${tbl.comment}';`);
    }
  }

  if (tbl.indexes) {
    for (const idx of tbl.indexes) {
      const uniq = idx.unique ? 'UNIQUE ' : '';
      const ifNotExists = dbType === 'mysql' ? '' : 'IF NOT EXISTS ';
      lines.push(`CREATE ${uniq}INDEX ${ifNotExists}${idx.name} ON ${tbl.name} (${idx.columns.join(', ')});`);
    }
  }

  return lines.join('\n');
}

// ── DB connection & introspection ──────────────────────────────────────────────
async function getExistingColumns(client, tableName, dbType) {
  if (dbType === 'postgresql') {
    const res = await client.query(
      `SELECT column_name FROM information_schema.columns
       WHERE table_schema = 'public' AND table_name = $1`, [tableName]);
    return new Set(res.rows.map(r => r.column_name));
  }
  if (dbType === 'mysql') {
    const [rows] = await client.query(
      `SELECT COLUMN_NAME as column_name FROM information_schema.COLUMNS
       WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = ?`, [tableName]);
    return new Set(rows.map(r => r.column_name));
  }
  if (dbType === 'sqlite') {
    const rows = await client.prepare(`PRAGMA table_info(${tableName})`).all();
    return new Set(rows.map(r => r.name));
  }
  return new Set();
}

async function tableExists(client, tableName, dbType) {
  if (dbType === 'postgresql') {
    const res = await client.query(
      `SELECT 1 FROM information_schema.tables
       WHERE table_schema = 'public' AND table_name = $1`, [tableName]);
    return res.rows.length > 0;
  }
  if (dbType === 'mysql') {
    const [rows] = await client.query(
      `SELECT 1 FROM information_schema.TABLES
       WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = ?`, [tableName]);
    return rows.length > 0;
  }
  if (dbType === 'sqlite') {
    const row = client.prepare(
      `SELECT 1 FROM sqlite_master WHERE type='table' AND name=?`).get(tableName);
    return !!row;
  }
  return false;
}

// ── DB package bootstrap ───────────────────────────────────────────────────────
const DB_PACKAGES = {
  postgresql: 'pg',
  mysql: 'mysql2',
  sqlite: 'better-sqlite3',
};

function isMissingRequestedModule(error, request, pkg) {
  if (error.code !== 'MODULE_NOT_FOUND') return false;
  return error.message.includes(`'${request}'`)
    || error.message.includes(`"${request}"`)
    || error.message.includes(`'${pkg}'`)
    || error.message.includes(`"${pkg}"`);
}

function installDbPackage(pkg) {
  console.log(`[gen-orm] Installing missing package "${pkg}"...`);
  try {
    execFileSync('npm', ['install', '--no-save', '--package-lock=false', pkg], {
      cwd: cfg.root,
      stdio: 'inherit',
      shell: process.platform === 'win32',
    });
  } catch (e) {
    e.genOrmInstallFailure = true;
    e.genOrmPackage = pkg;
    throw e;
  }
  console.log(`[gen-orm] Installed package "${pkg}".`);
}

function requireDbModule(dbType, request) {
  const pkg = DB_PACKAGES[dbType];
  try {
    return require(request);
  } catch (e) {
    if (!pkg || !isMissingRequestedModule(e, request, pkg)) throw e;
    installDbPackage(pkg);
    return require(request);
  }
}

// ── Connect to DB ──────────────────────────────────────────────────────────────
async function connectDB(dbType) {
  const { host, port, name, user, password, file } = cfg.db;
  const connectHost = host === 'localhost' ? '127.0.0.1' : host;
  try {
    if (dbType === 'postgresql') {
      const { Client } = requireDbModule(dbType, 'pg');
      const client = new Client({ host: connectHost, port, database: name, user, password });
      await client.connect();
      return {
        query: (sql, params) => client.query(sql, params),
        exec:  (sql) => client.query(sql),
        end:   () => client.end(),
        isAsync: true,
      };
    }
    if (dbType === 'mysql') {
      const mysql = requireDbModule(dbType, 'mysql2/promise');
      const conn  = await mysql.createConnection({ host: connectHost, port, database: name, user, password, multipleStatements: true });
      return {
        query: (sql, params) => conn.query(sql, params),
        exec:  (sql) => conn.query(sql),
        end:   () => conn.end(),
        isAsync: true,
      };
    }
    if (dbType === 'sqlite') {
      const DB = requireDbModule(dbType, 'better-sqlite3');
      const db = new DB(file || name + '.db');
      return {
        query:   () => { throw new Error('Use prepare().all() for SQLite'); },
        prepare: (sql) => db.prepare(sql),
        exec:    (sql) => db.exec(sql),
        end:     () => db.close(),
        isAsync: false,
      };
    }
    throw new Error(`Unsupported DB type "${dbType}". Supported: mysql`);
  } catch (e) {
    if (e.genOrmInstallFailure) {
      console.error(`[gen-orm] ERROR: Failed to install required package "${e.genOrmPackage}".`);
      console.error(`  Command: npm install --no-save --package-lock=false ${e.genOrmPackage}`);
      console.error(`  ${e.message}`);
    } else if (e.code === 'MODULE_NOT_FOUND') {
      const pkg = DB_PACKAGES[dbType];
      console.error(`[gen-orm] ERROR: Required package "${pkg}" could not be loaded.`);
      console.error(`  Tried automatic install: npm install --no-save --package-lock=false ${pkg}`);
    } else {
      console.error(`[gen-orm] ERROR: DB connection failed (${dbType}://${connectHost}:${port}/${name})`);
      if (connectHost !== host) console.error(`  Resolved host "${host}" -> "${connectHost}"`);
      console.error(`  ${e.message}`);
      console.error(`  Check ${path.relative(cfg.root, cfg.envPath)}: DB_BIND_ADDRESS/DB_PUBLISHED_PORT and DB_NAME/DB_USER/DB_PASSWORD`);
    }
    process.exit(1);
  }
}

// ── Main ───────────────────────────────────────────────────────────────────────
async function main() {
  const { dbSchema, migrationsDir } = cfg.paths;
  const dbType  = cfg.db.type;
  const dryRun  = cfg.ormGen.dryRun;

  if (!fs.existsSync(dbSchema)) {
    console.log('[gen-orm] No schema file found at', path.relative(cfg.root, dbSchema));
    console.log('  Create server/db/schema.json to define your DB tables.');
    return;
  }

  const rel = path.relative(cfg.root, dbSchema);
  let schema;
  try {
    schema = JSON.parse(fs.readFileSync(dbSchema, 'utf-8'));
  } catch (e) {
    console.error(`[gen-orm] ERROR: ${rel}\n  JSON parse error: ${e.message}`);
    process.exit(1);
  }

  const errors = validateSchema(schema, dbSchema);
  if (errors.length > 0) {
    console.error(`[gen-orm] ERROR: ${rel}`);
    for (const e of errors) console.error(`  ${e.loc}: ${e.msg}`);
    console.error(`\n${errors.length} error(s) found. Aborting.`);
    process.exit(1);
  }

  // ── Generate SQL ─────────────────────────────────────────────────────────
  const sqlLines    = [`-- Generated by gen-orm | DB: ${dbType} | ${new Date().toISOString()}`, ''];
  const alterLines  = [];
  const warnings    = [];

  let client;
  if (!dryRun) {
    client = await connectDB(dbType);
  }

  for (const tbl of schema.tables) {
    const exists = client ? await tableExists(client, tbl.name, dbType) : false;

    if (!exists) {
      const createSQL = generateCreateTable(tbl, dbType);
      sqlLines.push(createSQL, '');
      if (client) {
        try {
          await client.exec(createSQL);
          console.log(`[gen-orm] CREATED: ${tbl.name}`);
        } catch (e) {
          console.error(`[gen-orm] ERROR creating table "${tbl.name}": ${e.message}`);
        }
      } else {
        console.log(`[gen-orm] (dry-run) Would CREATE: ${tbl.name}`);
      }
    } else {
      const existingCols = client
        ? await getExistingColumns(client, tbl.name, dbType)
        : new Set();

      for (const col of tbl.columns) {
        if (!existingCols.has(col.name) || !client) {
          const colSQL = buildColumnSQL(col, dbType);
          const alterSQL = dbType === 'mysql'
            ? `ALTER TABLE ${tbl.name} ADD COLUMN ${colSQL};`
            : `ALTER TABLE ${tbl.name} ADD COLUMN IF NOT EXISTS ${colSQL};`;
          alterLines.push(alterSQL);
          if (client) {
            try {
              await client.exec(alterSQL);
              console.log(`[gen-orm] ADDED column: ${tbl.name}.${col.name}`);
            } catch (e) {
              warnings.push(`Column ${tbl.name}.${col.name}: ${e.message}`);
            }
          } else {
            console.log(`[gen-orm] (dry-run) Would ADD column: ${tbl.name}.${col.name}`);
          }
        }
      }

      // Warn about type changes (never auto-alter)
      if (client) {
        for (const col of tbl.columns) {
          if (existingCols.has(col.name)) {
            warnings.push(
              `Table "${tbl.name}", Column "${col.name}": type change detected — alter manually if needed`
            );
          }
        }
      }
    }
  }

  if (client) await client.end();

  // ── Save migration SQL file ───────────────────────────────────────────────
  if (alterLines.length) sqlLines.push(...alterLines, '');

  const ts        = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
  const sqlFile   = path.join(migrationsDir, `${ts}_schema_sync.sql`);
  fs.mkdirSync(migrationsDir, { recursive: true });
  fs.writeFileSync(sqlFile, sqlLines.join('\n'), 'utf-8');
  console.log(`[gen-orm] Migration SQL: ${path.relative(cfg.root, sqlFile)}`);

  if (warnings.length) {
    console.warn('\n[gen-orm] WARNINGS:');
    for (const w of warnings) console.warn('  ' + w);
  }

  if (dryRun) {
    console.log('\n[gen-orm] Dry-run mode — SQL generated but NOT executed.');
    console.log('  Set orm-gen.dry_run = false in template.ini to execute on DB.');
  } else {
    console.log(`[gen-orm] Done: ${schema.tables.length} table(s) synced.`);
  }
}

main().catch(e => {
  console.error('[gen-orm] Unexpected error:', e.message);
  if (e.stack) console.error(e.stack);
  process.exit(1);
});

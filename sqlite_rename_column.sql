-- SQLite commands to rename port of registry column to shipyard of registry
-- Run these commands in order

-- First, check the current table structure
PRAGMA table_info(Ships);

-- Method 1: For SQLite 3.25.0+ (newer versions)
-- Try this first:
ALTER TABLE Ships RENAME COLUMN PortOfRegistry TO ShipyardOfRegistry;
-- OR if the column is named with underscores:
-- ALTER TABLE Ships RENAME COLUMN port_of_registry TO shipyard_of_registry;

-- Method 2: For older SQLite versions (if Method 1 doesn't work)
-- You'll need to recreate the table:

-- Step 1: Create new table with the correct column name
CREATE TABLE Ships_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ShipName TEXT NOT NULL,
    ImoNumber TEXT NOT NULL,
    ShipType TEXT,
    Flag TEXT,
    ShipyardOfRegistry TEXT,  -- This is the renamed column
    Class TEXT,
    ClassNotation TEXT,
    BuildYear INTEGER,
    GrossTonnage REAL,
    NetTonnage REAL,
    DeadweightTonnage REAL,
    Owner TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

-- Step 2: Copy data from old table to new table
INSERT INTO Ships_new (
    Id, ShipName, ImoNumber, ShipType, Flag, ShipyardOfRegistry, 
    Class, ClassNotation, BuildYear, GrossTonnage, NetTonnage, 
    DeadweightTonnage, Owner, IsActive, IsDeleted, CreatedAt, UpdatedAt
)
SELECT 
    Id, ShipName, ImoNumber, ShipType, Flag, PortOfRegistry, 
    Class, ClassNotation, BuildYear, GrossTonnage, NetTonnage, 
    DeadweightTonnage, Owner, IsActive, IsDeleted, CreatedAt, UpdatedAt
FROM Ships;

-- Step 3: Drop old table
DROP TABLE Ships;

-- Step 4: Rename new table
ALTER TABLE Ships_new RENAME TO Ships;

-- Verify the change
PRAGMA table_info(Ships); 
-- Migration to update SecurityZone from foreign key to text field
-- This migration converts the SecurityZoneId foreign key to a SecurityZone text field

-- Step 1: Add the new SecurityZone text column
ALTER TABLE Systems ADD COLUMN SecurityZone TEXT;

-- Step 2: Migrate existing data from SecurityZoneId to SecurityZone text
UPDATE Systems 
SET SecurityZone = (
    SELECT sz.Name 
    FROM SecurityZones sz 
    WHERE sz.Id = Systems.SecurityZoneId
);

-- Step 3: Drop the foreign key constraint and SecurityZoneId column
-- Note: SQLite doesn't support DROP COLUMN directly, so we need to recreate the table
-- Create a backup table with the new structure
CREATE TABLE Systems_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ShipId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Manufacturer TEXT NOT NULL,
    Model TEXT NOT NULL,
    SerialNumber TEXT NOT NULL,
    Description TEXT,
    SecurityZone TEXT,
    HasRemoteConnection BOOLEAN NOT NULL DEFAULT 0,
    CategoryId INTEGER NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME,
    FOREIGN KEY (ShipId) REFERENCES Ships(Id),
    FOREIGN KEY (CategoryId) REFERENCES SystemCategories(Id)
);

-- Copy data to the new table
INSERT INTO Systems_new (Id, ShipId, Name, Manufacturer, Model, SerialNumber, Description, SecurityZone, HasRemoteConnection, CategoryId, CreatedAt, UpdatedAt)
SELECT Id, ShipId, Name, Manufacturer, Model, SerialNumber, Description, SecurityZone, HasRemoteConnection, CategoryId, CreatedAt, UpdatedAt
FROM Systems;

-- Drop the old table and rename the new one
DROP TABLE Systems;
ALTER TABLE Systems_new RENAME TO Systems;

-- Recreate the unique index on SerialNumber
CREATE UNIQUE INDEX IX_Systems_SerialNumber ON Systems(SerialNumber);

-- Step 4: Drop the SecurityZones table as it's no longer needed
DROP TABLE SecurityZones; 
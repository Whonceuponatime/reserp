-- Migration: Update Security Zone to Optional Text Field and Update System Categories
-- Date: 2024-12-19

BEGIN TRANSACTION;

-- Step 1: Add new SecurityZoneText column to Systems table
ALTER TABLE Systems ADD COLUMN SecurityZoneText TEXT;

-- Step 2: Migrate existing SecurityZone data to text field
UPDATE Systems 
SET SecurityZoneText = (
    SELECT sz.Name 
    FROM SecurityZones sz 
    WHERE sz.Id = Systems.SecurityZoneId
);

-- Step 3: Drop the foreign key constraint and SecurityZoneId column
-- Note: SQLite doesn't support dropping columns directly, so we need to recreate the table

-- Create new Systems table without SecurityZoneId
CREATE TABLE Systems_New (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ShipId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Manufacturer TEXT NOT NULL,
    Model TEXT NOT NULL,
    SerialNumber TEXT NOT NULL UNIQUE,
    Description TEXT,
    HasRemoteConnection BOOLEAN NOT NULL DEFAULT 0,
    SecurityZoneText TEXT, -- New optional text field
    CategoryId INTEGER NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME,
    FOREIGN KEY (ShipId) REFERENCES Ships(Id),
    FOREIGN KEY (CategoryId) REFERENCES SystemCategories(Id)
);

-- Copy data to new table
INSERT INTO Systems_New (Id, ShipId, Name, Manufacturer, Model, SerialNumber, Description, HasRemoteConnection, SecurityZoneText, CategoryId, CreatedAt, UpdatedAt)
SELECT Id, ShipId, Name, Manufacturer, Model, SerialNumber, Description, HasRemoteConnection, SecurityZoneText, CategoryId, CreatedAt, UpdatedAt
FROM Systems;

-- Drop old table and rename new one
DROP TABLE Systems;
ALTER TABLE Systems_New RENAME TO Systems;

-- Recreate indexes
CREATE INDEX IX_Systems_ShipId ON Systems(ShipId);
CREATE INDEX IX_Systems_CategoryId ON Systems(CategoryId);

-- Step 4: Update System Categories with new categories
DELETE FROM SystemCategories;

INSERT INTO SystemCategories (Id, Name, Description) VALUES 
(1, 'Propulsion', 'Propulsion systems including main engines and propulsion control'),
(2, 'Steering', 'Steering systems and rudder control'),
(3, 'Anchoring and mooring', 'Anchoring, mooring, and positioning systems'),
(4, 'Electrical power generation and distribution', 'Electrical power generation, distribution, and power management systems'),
(5, 'Fire detection and extinguishing systems', 'Fire detection, alarm, and suppression systems'),
(6, 'Bilge and ballast systems, loading computer', 'Bilge pumps, ballast systems, and cargo loading computers'),
(7, 'Watertight integrity and flooding detection', 'Watertight doors, flooding detection, and hull integrity monitoring'),
(8, 'Lighting (e.g. emergency lighting, low locations, navigation lights, etc.)', 'All lighting systems including emergency, navigation, and general illumination'),
(9, 'Safety Systems', 'General safety systems and emergency equipment'),
(10, 'Navigational systems required by statutory regulations', 'Navigation equipment required by SOLAS and other maritime regulations'),
(11, 'Internal and external communication systems required by class rules and statutory regulations', 'Communication systems required by classification societies and maritime regulations');

-- Step 5: Drop SecurityZones table as it's no longer needed
-- (Keep it for now in case we need to reference historical data)
-- DROP TABLE SecurityZones;

COMMIT; 
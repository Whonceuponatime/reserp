-- Apply all database migrations
-- This script adds the SecurityZone column to Systems table and StatusId column to Documents table

-- 1. Add SecurityZone column to Systems table
ALTER TABLE Systems ADD COLUMN SecurityZone TEXT;

-- 2. Add StatusId column to Documents table  
ALTER TABLE Documents ADD COLUMN StatusId INTEGER;

-- 3. Create DocumentStatus table if it doesn't exist
CREATE TABLE IF NOT EXISTS DocumentStatus (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 4. Insert initial DocumentStatus data
INSERT OR IGNORE INTO DocumentStatus (Id, Name, Description) VALUES 
(1, 'Pending', 'Document is pending review'),
(2, 'Approved', 'Document has been approved'),
(3, 'Rejected', 'Document has been rejected');

-- 5. Update existing documents to have StatusId = 1 (Pending) where StatusId is NULL
UPDATE Documents SET StatusId = 1 WHERE StatusId IS NULL;

-- 6. Update existing documents based on IsApproved field
UPDATE Documents SET StatusId = 2 WHERE IsApproved = 1;

PRAGMA foreign_keys=off;
PRAGMA foreign_keys=on; 
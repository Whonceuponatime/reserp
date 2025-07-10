-- Manual Database Creation Script for Maritime ERP
-- Run this script if the automatic database initialization fails

-- Create HardwareChangeRequests table
CREATE TABLE IF NOT EXISTS HardwareChangeRequests (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RequestNumber TEXT NOT NULL UNIQUE,
    CreatedDate DATETIME NOT NULL,
    RequesterUserId INTEGER NOT NULL,
    Department TEXT,
    PositionTitle TEXT,
    RequesterName TEXT,
    InstalledCbs TEXT,
    InstalledComponent TEXT,
    Reason TEXT,
    BeforeHwManufacturerModel TEXT,
    BeforeHwName TEXT,
    BeforeHwOs TEXT,
    AfterHwManufacturerModel TEXT,
    AfterHwName TEXT,
    AfterHwOs TEXT,
    WorkDescription TEXT,
    SecurityReviewComment TEXT,
    PreparedByUserId INTEGER,
    ReviewedByUserId INTEGER,
    ApprovedByUserId INTEGER,
    PreparedAt DATETIME,
    ReviewedAt DATETIME,
    ApprovedAt DATETIME,
    Status TEXT NOT NULL DEFAULT 'Draft',
    FOREIGN KEY (RequesterUserId) REFERENCES Users(Id),
    FOREIGN KEY (PreparedByUserId) REFERENCES Users(Id),
    FOREIGN KEY (ReviewedByUserId) REFERENCES Users(Id),
    FOREIGN KEY (ApprovedByUserId) REFERENCES Users(Id)
);

-- Create SystemChangePlans table (if not exists)
CREATE TABLE IF NOT EXISTS SystemChangePlans (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RequestNumber TEXT NOT NULL UNIQUE,
    CreatedDate DATETIME NOT NULL,
    UserId INTEGER,
    RequesterName TEXT,
    Department TEXT,
    PositionTitle TEXT,
    Reason TEXT,
    BeforeHwSwName TEXT,
    BeforeHwSwManufacturerModel TEXT,
    BeforeHwSwOs TEXT,
    AfterHwSwName TEXT,
    AfterHwSwManufacturerModel TEXT,
    AfterHwSwOs TEXT,
    WorkDescription TEXT,
    SecurityReviewComment TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft',
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS IX_HardwareChangeRequests_RequestNumber ON HardwareChangeRequests(RequestNumber);
CREATE INDEX IF NOT EXISTS IX_HardwareChangeRequests_RequesterUserId ON HardwareChangeRequests(RequesterUserId);
CREATE INDEX IF NOT EXISTS IX_HardwareChangeRequests_Status ON HardwareChangeRequests(Status);
CREATE INDEX IF NOT EXISTS IX_HardwareChangeRequests_CreatedDate ON HardwareChangeRequests(CreatedDate);

CREATE INDEX IF NOT EXISTS IX_SystemChangePlans_RequestNumber ON SystemChangePlans(RequestNumber);
CREATE INDEX IF NOT EXISTS IX_SystemChangePlans_UserId ON SystemChangePlans(UserId);
CREATE INDEX IF NOT EXISTS IX_SystemChangePlans_Status ON SystemChangePlans(Status);
CREATE INDEX IF NOT EXISTS IX_SystemChangePlans_CreatedDate ON SystemChangePlans(CreatedDate);

-- Verify tables were created
SELECT 'HardwareChangeRequests table created' as message WHERE EXISTS (
    SELECT 1 FROM sqlite_master WHERE type='table' AND name='HardwareChangeRequests'
);

SELECT 'SystemChangePlans table created' as message WHERE EXISTS (
    SELECT 1 FROM sqlite_master WHERE type='table' AND name='SystemChangePlans'
);

-- Show all tables
SELECT name FROM sqlite_master WHERE type='table' ORDER BY name; 
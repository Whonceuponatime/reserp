-- Create HardwareChangeRequests table
CREATE TABLE HardwareChangeRequests (
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

-- Create index on RequestNumber for better performance
CREATE INDEX IX_HardwareChangeRequests_RequestNumber ON HardwareChangeRequests(RequestNumber);

-- Create index on RequesterUserId for better performance
CREATE INDEX IX_HardwareChangeRequests_RequesterUserId ON HardwareChangeRequests(RequesterUserId);

-- Create index on Status for better performance
CREATE INDEX IX_HardwareChangeRequests_Status ON HardwareChangeRequests(Status); 
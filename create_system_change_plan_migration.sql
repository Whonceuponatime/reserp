-- Create SystemChangePlans table
CREATE TABLE IF NOT EXISTS SystemChangePlans (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RequestNumber NVARCHAR(50) NOT NULL UNIQUE,
    CreatedDate DATETIME NOT NULL,
    IsCreated BOOLEAN NOT NULL DEFAULT 1,
    IsUnderReview BOOLEAN NOT NULL DEFAULT 0,
    IsApproved BOOLEAN NOT NULL DEFAULT 0,
    Department NVARCHAR(100) NOT NULL DEFAULT '',
    PositionTitle NVARCHAR(100) NOT NULL DEFAULT '',
    RequesterName NVARCHAR(100) NOT NULL DEFAULT '',
    InstalledCbs NVARCHAR(200) NOT NULL DEFAULT '',
    InstalledComponent NVARCHAR(200) NOT NULL DEFAULT '',
    Reason TEXT NOT NULL DEFAULT '',
    BeforeManufacturerModel NVARCHAR(200) NOT NULL DEFAULT '',
    BeforeHwSwName NVARCHAR(200) NOT NULL DEFAULT '',
    BeforeVersion NVARCHAR(100) NOT NULL DEFAULT '',
    AfterManufacturerModel NVARCHAR(200) NOT NULL DEFAULT '',
    AfterHwSwName NVARCHAR(200) NOT NULL DEFAULT '',
    AfterVersion NVARCHAR(100) NOT NULL DEFAULT '',
    PlanDetails TEXT NOT NULL DEFAULT '',
    SecurityReviewComments TEXT NOT NULL DEFAULT '',
    UserId INTEGER NULL,
    UpdatedDate DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- Create index on RequestNumber for performance
CREATE INDEX IF NOT EXISTS IX_SystemChangePlans_RequestNumber ON SystemChangePlans(RequestNumber); 
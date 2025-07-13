-- SQL commands to rename port_of_registry to shipyard_of_registry
-- Run these commands in order

-- Method 1: Using ALTER TABLE RENAME COLUMN (for SQL Server 2017+ or other modern databases)
-- If this doesn't work, use Method 2 below

-- Check current column name first
SELECT COLUMN_NAME, TABLE_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Ships' AND COLUMN_NAME IN ('PortOfRegistry', 'port_of_registry');

-- Option 1: If column is named 'PortOfRegistry'
EXEC sp_rename 'Ships.PortOfRegistry', 'ShipyardOfRegistry', 'COLUMN';

-- Option 2: If column is named 'port_of_registry' 
EXEC sp_rename 'Ships.port_of_registry', 'shipyard_of_registry', 'COLUMN';

-- Method 2: Alternative approach using ADD/DROP (if rename doesn't work)
-- Uncomment and use this if the above doesn't work:

/*
-- Step 1: Add the new column
ALTER TABLE Ships ADD shipyard_of_registry NVARCHAR(200);

-- Step 2: Copy data from old column to new column
UPDATE Ships SET shipyard_of_registry = PortOfRegistry;
-- OR if column is named port_of_registry:
-- UPDATE Ships SET shipyard_of_registry = port_of_registry;

-- Step 3: Drop the old column
ALTER TABLE Ships DROP COLUMN PortOfRegistry;
-- OR if column is named port_of_registry:
-- ALTER TABLE Ships DROP COLUMN port_of_registry;
*/

-- Verify the change
SELECT COLUMN_NAME, TABLE_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Ships' AND COLUMN_NAME LIKE '%registry%'; 
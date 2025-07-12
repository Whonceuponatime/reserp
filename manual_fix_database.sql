-- Add SecurityZone column if it doesn't exist
-- Run this in any SQLite browser or tool

ALTER TABLE Systems ADD COLUMN SecurityZone TEXT;

-- Update system categories (if needed)
UPDATE SystemCategories SET Name = 'Propulsion', Description = 'Propulsion systems' WHERE Id = 1;
UPDATE SystemCategories SET Name = 'Steering', Description = 'Steering systems' WHERE Id = 2;
UPDATE SystemCategories SET Name = 'Anchoring and mooring', Description = 'Anchoring and mooring systems' WHERE Id = 3;
UPDATE SystemCategories SET Name = 'Electrical power generation and distribution', Description = 'Electrical power generation and distribution systems' WHERE Id = 4;
UPDATE SystemCategories SET Name = 'Fire detection and extinguishing systems', Description = 'Fire detection and extinguishing systems' WHERE Id = 5;
UPDATE SystemCategories SET Name = 'Bilge and ballast systems, loading computer', Description = 'Bilge and ballast systems, loading computer' WHERE Id = 6;
UPDATE SystemCategories SET Name = 'Watertight integrity and flooding detection', Description = 'Watertight integrity and flooding detection systems' WHERE Id = 7;
UPDATE SystemCategories SET Name = 'Lighting (e.g. emergency lighting, low locations, navigation lights, etc.)', Description = 'Lighting systems including emergency lighting, low locations, navigation lights, etc.' WHERE Id = 8;
UPDATE SystemCategories SET Name = 'Safety Systems', Description = 'Safety systems' WHERE Id = 9;
UPDATE SystemCategories SET Name = 'Navigational systems required by statutory regulations', Description = 'Navigational systems required by statutory regulations' WHERE Id = 10;
UPDATE SystemCategories SET Name = 'Internal and external communication systems required by class rules and statutory regulations', Description = 'Internal and external communication systems required by class rules and statutory regulations' WHERE Id = 11; 
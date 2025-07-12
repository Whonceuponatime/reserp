-- Create or update admin user
-- First, ensure the Administrator role exists
INSERT OR IGNORE INTO Roles (Id, Name, Description) VALUES (1, 'Administrator', 'Full system access - can manage users, approve/reject forms, edit all data');

-- Create a properly hashed password for 'admin123'
-- This is a BCrypt hash of 'admin123'
INSERT OR REPLACE INTO Users (Id, Username, PasswordHash, FullName, Email, RoleId, IsActive, CreatedAt)
VALUES (1, 'admin', '$2a$11$rOZKqXhwJzJqLZ6Y8HQUCOyDJHZxKQCqtFZUbGTwJdpXYWUCdxJcG', 'System Administrator', 'admin@maritime.com', 1, 1, datetime('now'));

-- Verify the user was created
SELECT 'Admin user created/updated successfully' as message;
SELECT u.Id, u.Username, u.FullName, u.Email, r.Name as Role, u.IsActive 
FROM Users u 
JOIN Roles r ON u.RoleId = r.Id 
WHERE u.Username = 'admin'; 
# 📋 Functional Design Specification - Maritime ERP System
## CRUD Operations Focus

## 🔍 Document Overview

**Document Title:** Maritime ERP System - CRUD Operations Functional Design Specification  
**Version:** 2.0 - CRUD Focus  
**Date:** December 2024  
**System Name:** SEACURE(CARE) - Maritime Enterprise Resource Planning System  
**Focus:** Create, Read, Update, Delete Operations for Core Entities  

---

## 📖 Table of Contents

1. [System Overview](#system-overview)
2. [CRUD Architecture Overview](#crud-architecture-overview)
3. [Ships CRUD Operations](#ships-crud-operations)
4. [Systems CRUD Operations](#systems-crud-operations)
5. [Components CRUD Operations](#components-crud-operations)
6. [Software CRUD Operations](#software-crud-operations)
7. [Change Requests CRUD Operations](#change-requests-crud-operations)
8. [Documents CRUD Operations](#documents-crud-operations)
9. [User Management CRUD Operations](#user-management-crud-operations)
10. [Audit Logs Operations](#audit-logs-operations)
11. [Data Relationships & Integrity](#data-relationships--integrity)
12. [CRUD Implementation Patterns](#crud-implementation-patterns)

---

## 🌊 System Overview

### Core Entities
The Maritime ERP system manages eight core entities with full CRUD capabilities:

1. **Ships** - Fleet vessel management
2. **Systems** - Ship-based systems and equipment
3. **Components** - Hardware components within systems
4. **Software** - Software installed on components
5. **Change Requests** - Change management workflow
6. **Documents** - File and document management
7. **Users** - User account management
8. **Audit Logs** - System activity tracking

### CRUD Operation Scope
Each entity supports the following operations:
- **Create**: Add new records with validation
- **Read**: Retrieve records with filtering and search
- **Update**: Modify existing records with audit trail
- **Delete**: Remove records (soft delete where applicable)

---

## 🏗️ CRUD Architecture Overview

### Three-Layer Architecture
```
🎨 UI Layer (Views & ViewModels)
    ↓ CRUD Commands
🔧 Business Logic Layer (Services)
    ↓ Data Operations
🗃️ Data Access Layer (Entity Framework Core)
    ↓ Database Operations
💾 SQLite Database
```

### CRUD Flow Pattern
```
User Action → ViewModel Command → Service Method → Entity Framework → Database
Database Response → Entity Framework → Service Response → ViewModel Update → UI Refresh
```

---

## ⚓ Ships CRUD Operations

### Entity Definition
```csharp
public class Ship
{
    public int Id { get; set; }
    public string ShipName { get; set; }
    public string ImoNumber { get; set; }
    public string ShipType { get; set; }
    public string Flag { get; set; }
    public string Class { get; set; }
    public decimal? GrossTonnage { get; set; }
    public short? BuildYear { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation Properties
    public ICollection<ShipSystem> Systems { get; set; }
    public ICollection<Document> Documents { get; set; }
    public ICollection<ChangeRequest> ChangeRequests { get; set; }
}
```

### CRUD Operations

#### **Create Ship**
- **UI**: ShipsView → "Add Ship" button → ShipEditDialog
- **Validation**: IMO number uniqueness, required fields
- **Service**: `ShipService.CreateShipAsync(Ship ship)`
- **Audit**: Log ship creation with user details
- **Business Rules**:
  - IMO number must be unique and 7 characters
  - Ship name is required
  - Build year must be reasonable (1900-2030)

#### **Read Ships**
- **UI**: ShipsView with DataGrid and search/filter controls
- **Service Methods**:
  - `GetAllShipsAsync()` - All active ships
  - `GetShipByIdAsync(int id)` - Single ship with details
  - `SearchShipsAsync(string searchTerm)` - Text search
  - `GetShipsByTypeAsync(string shipType)` - Filter by type
  - `GetShipsByFlagAsync(string flag)` - Filter by flag
- **Performance**: Lazy loading for navigation properties
- **Sorting**: By ship name, IMO number, build year

#### **Update Ship**
- **UI**: ShipsView → "Edit Ship" button → ShipEditDialog
- **Service**: `ShipService.UpdateShipAsync(Ship ship)`
- **Validation**: Same as create, excluding current ship from IMO uniqueness
- **Audit**: Log changes with old/new values
- **Optimistic Concurrency**: UpdatedAt timestamp check

#### **Delete Ship**
- **UI**: ShipsView → "Delete Ship" button → Confirmation dialog
- **Service**: `ShipService.DeleteShipAsync(int id)`
- **Soft Delete**: Sets IsDeleted = true, preserves data
- **Cascade Check**: Verify no active systems before deletion
- **Audit**: Log deletion with reason

---

## ⚙️ Systems CRUD Operations

### Entity Definition
```csharp
public class ShipSystem
{
    public int Id { get; set; }
    public int ShipId { get; set; }
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string SerialNumber { get; set; }
    public string Description { get; set; }
    public bool HasRemoteConnection { get; set; }
    public string SecurityZone { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public Ship Ship { get; set; }
    public SystemCategory Category { get; set; }
    public ICollection<Component> Components { get; set; }
}
```

### CRUD Operations

#### **Create System**
- **UI**: SystemsView → "Add System" button → SystemEditDialog
- **Parent Relationship**: Must belong to an existing ship
- **Service**: `SystemService.CreateSystemAsync(ShipSystem system)`
- **Validation**: Serial number uniqueness within ship
- **Business Rules**:
  - System must be assigned to a ship
  - Serial number required and unique
  - Category must be valid

#### **Read Systems**
- **UI**: SystemsView with hierarchical display (Ship → Systems)
- **Service Methods**:
  - `GetAllSystemsAsync()` - All systems with ship details
  - `GetSystemByIdAsync(int id)` - Single system with components
  - `GetSystemsByShipIdAsync(int shipId)` - Systems for specific ship
  - `GetSystemsByCategoryAsync(int categoryId)` - Filter by category
  - `GetSystemsByManufacturerAsync(string manufacturer)` - Filter by manufacturer
- **Include Strategy**: Always include Ship and Category

#### **Update System**
- **UI**: SystemsView → "Edit System" button → SystemEditDialog
- **Service**: `SystemService.UpdateSystemAsync(ShipSystem system)`
- **Validation**: Serial number uniqueness excluding current system
- **Audit**: Track system modifications

#### **Delete System**
- **UI**: SystemsView → "Delete System" button → Confirmation dialog
- **Service**: `SystemService.DeleteSystemAsync(int id)`
- **Cascade Check**: Verify no components exist before deletion
- **Hard Delete**: Permanently removes record after validation

---

## 🔧 Components CRUD Operations

### Entity Definition
```csharp
public class Component
{
    public int Id { get; set; }
    public int SystemId { get; set; }
    public string SystemName { get; set; }
    public string ComponentType { get; set; }
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string InstalledLocation { get; set; }
    public string OsName { get; set; }
    public string OsVersion { get; set; }
    public short LanPorts { get; set; }
    public short UsbPorts { get; set; }
    public string SupportedProtocols { get; set; }
    public string NetworkSegment { get; set; }
    public bool RemoteConnection { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public ShipSystem System { get; set; }
    public ICollection<Software> Software { get; set; }
}
```

### CRUD Operations

#### **Create Component**
- **UI**: ComponentsView → "Add Component" button → ComponentEditDialog
- **Parent Relationship**: Must belong to an existing system
- **Service**: `ComponentService.AddComponentAsync(Component component)`
- **Validation**: Component name uniqueness within system
- **Business Rules**:
  - Component must be assigned to a system
  - Port counts must be non-negative
  - Network segment validation if has remote connection

#### **Read Components**
- **UI**: ComponentsView with system filtering and search
- **Service Methods**:
  - `GetAllComponentsAsync()` - All components with system details
  - `GetComponentByIdAsync(int id)` - Single component with software
  - `GetComponentsBySystemIdAsync(int systemId)` - Components for specific system
  - `GetComponentsByLocationAsync(string location)` - Filter by location
  - `GetComponentsWithRemoteConnectionAsync()` - Security-relevant components
- **Include Strategy**: Always include System and Ship details

#### **Update Component**
- **UI**: ComponentsView → "Edit Component" button → ComponentEditDialog
- **Service**: `ComponentService.UpdateComponentAsync(Component component)`
- **Validation**: Name uniqueness within system, excluding current component
- **Audit**: Track component modifications

#### **Delete Component**
- **UI**: ComponentsView → "Delete Component" button → Confirmation dialog
- **Service**: `ComponentService.DeleteComponentAsync(int id)`
- **Cascade Check**: Verify no software installed before deletion
- **Hard Delete**: Permanently removes record after validation

---

## 💻 Software CRUD Operations

### Entity Definition
```csharp
public class Software
{
    public int Id { get; set; }
    public string Manufacturer { get; set; }
    public string Name { get; set; }
    public string SoftwareType { get; set; }
    public string Version { get; set; }
    public string FunctionPurpose { get; set; }
    public string InstalledHardwareComponent { get; set; }
    public int? InstalledComponentId { get; set; }
    public string LicenseType { get; set; }
    public string LicenseKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public Component InstalledComponent { get; set; }
}
```

### CRUD Operations

#### **Create Software**
- **UI**: SoftwareView → "Add Software" button → SoftwareEditDialog
- **Parent Relationship**: Must be installed on an existing component
- **Service**: `SoftwareService.AddSoftwareAsync(Software software)`
- **Validation**: Software name + version uniqueness on component
- **Business Rules**:
  - Software must be installed on a component
  - License key validation for commercial software
  - Version format validation

#### **Read Software**
- **UI**: SoftwareView with component filtering and search
- **Service Methods**:
  - `GetAllSoftwareAsync()` - All software with component details
  - `GetSoftwareByIdAsync(int id)` - Single software entry
  - `GetSoftwareByComponentIdAsync(int componentId)` - Software on specific component
  - `SearchSoftwareAsync(string searchTerm)` - Text search
  - `GetManufacturersAsync()` - List of manufacturers
- **Include Strategy**: Always include Component and System details

#### **Update Software**
- **UI**: SoftwareView → "Edit Software" button → SoftwareEditDialog
- **Service**: `SoftwareService.UpdateSoftwareAsync(Software software)`
- **Validation**: Name + version uniqueness on component, excluding current software
- **Audit**: Track software modifications

#### **Delete Software**
- **UI**: SoftwareView → "Delete Software" button → Confirmation dialog
- **Service**: `SoftwareService.DeleteSoftwareAsync(int id)`
- **Hard Delete**: Permanently removes record
- **No Cascade**: Software is leaf node in hierarchy

---

## 📋 Change Requests CRUD Operations

### Entity Definition
```csharp
public class ChangeRequest
{
    public int Id { get; set; }
    public string RequestNo { get; set; }
    public int? ShipId { get; set; }
    public int RequestTypeId { get; set; }
    public int StatusId { get; set; }
    public int RequestedById { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Purpose { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public Ship Ship { get; set; }
    public ChangeType RequestType { get; set; }
    public ChangeStatus Status { get; set; }
    public User RequestedBy { get; set; }
    public ICollection<Approval> Approvals { get; set; }
}
```

### CRUD Operations

#### **Create Change Request**
- **UI**: ChangeRequestsView → "New Change Request" button → ChangeRequestTypeSelectionDialog
- **Workflow**: Select type → Fill form → Submit for approval
- **Service**: `ChangeRequestService.CreateChangeRequestAsync(ChangeRequest request)`
- **Business Rules**:
  - Auto-generate unique request number
  - Initial status = "Created"
  - Current user as requester
  - Purpose and description required

#### **Read Change Requests**
- **UI**: ChangeRequestsView with status filtering and search
- **Service Methods**:
  - `GetAllChangeRequestsAsync()` - All requests with full details
  - `GetChangeRequestByIdAsync(int id)` - Single request with approvals
  - `GetChangeRequestsByShipAsync(int shipId)` - Ship-specific requests
  - `GetChangeRequestsByStatusAsync(int statusId)` - Filter by status
  - `GetChangeRequestsByUserAsync(int userId)` - User's requests
  - `GetPendingApprovalsAsync(int userId)` - Requests awaiting user approval
- **Include Strategy**: Include Ship, RequestType, Status, RequestedBy

#### **Update Change Request**
- **UI**: ChangeRequestsView → "Edit Request" button → Appropriate form dialog
- **Service**: `ChangeRequestService.UpdateChangeRequestAsync(ChangeRequest request)`
- **Validation**: Only editable in "Created" or "Under Review" status
- **Audit**: Track request modifications

#### **Delete Change Request**
- **UI**: ChangeRequestsView → "Delete Request" button → Confirmation dialog
- **Service**: `ChangeRequestService.DeleteChangeRequestAsync(int id)`
- **Business Rules**: Only deletable in "Created" status
- **Hard Delete**: Permanently removes record and related data

#### **Workflow Operations**
- **Submit for Approval**: `SubmitForApprovalAsync(int requestId, int userId)`
- **Approve**: `ApproveChangeRequestAsync(int requestId, int userId, string comment)`
- **Reject**: `RejectChangeRequestAsync(int requestId, int userId, string comment)`
- **Implement**: `ImplementChangeRequestAsync(int requestId, int userId)`

---

## 📄 Documents CRUD Operations

### Entity Definition
```csharp
public class Document
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public int CategoryId { get; set; }
    public int? ShipId { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Comments { get; set; }
    public int Version { get; set; }
    
    // Navigation Properties
    public DocumentCategory Category { get; set; }
    public Ship Ship { get; set; }
    public User UploadedBy { get; set; }
    public User ApprovedBy { get; set; }
}
```

### CRUD Operations

#### **Create Document**
- **UI**: DocumentsView → "Upload Document" button → DocumentUploadDialog
- **File Handling**: Select file → Validate → Upload → Create metadata
- **Service**: `DocumentService.CreateDocumentAsync(Document document, Stream fileStream, string originalFileName)`
- **Business Rules**:
  - File type validation (PDF, DOC, XLS, images)
  - File size limits (configurable)
  - Duplicate detection via MD5 hash
  - Category assignment required

#### **Read Documents**
- **UI**: DocumentsView with category and ship filtering
- **Service Methods**:
  - `GetAllDocumentsAsync()` - All active documents
  - `GetDocumentByIdAsync(int id)` - Single document metadata
  - `GetDocumentsByShipIdAsync(int shipId)` - Ship-specific documents
  - `GetDocumentsByCategoryIdAsync(int categoryId)` - Category-specific documents
  - `GetFileStreamAsync(string filePath)` - Download file content
- **Include Strategy**: Include Category, Ship, UploadedBy, ApprovedBy

#### **Update Document**
- **UI**: DocumentsView → "Edit Document" button → DocumentEditDialog
- **Service**: `DocumentService.UpdateDocumentAsync(Document document)`
- **Validation**: Only metadata updates, not file content
- **Audit**: Track document modifications

#### **Delete Document**
- **UI**: DocumentsView → "Delete Document" button → Confirmation dialog
- **Service**: `DocumentService.DeleteDocumentAsync(int id)`
- **Soft Delete**: Sets IsActive = false
- **File Cleanup**: Optionally remove physical file

#### **Approval Operations**
- **Approve**: `ApproveDocumentAsync(int documentId, int approvedByUserId, string comments)`
- **Reject**: `RejectDocumentAsync(int documentId, int rejectedByUserId, string comments)`

---

## 👥 User Management CRUD Operations

### Entity Definition
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    
    // Navigation Properties
    public Role Role { get; set; }
    public ICollection<ChangeRequest> ChangeRequests { get; set; }
    public ICollection<Document> UploadedDocuments { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; }
}
```

### CRUD Operations

#### **Create User**
- **UI**: UserManagementView → "Add User" button → UserEditDialog
- **Access Control**: Administrator role required
- **Service**: `UserService.CreateUserAsync(User user, string password)`
- **Business Rules**:
  - Username uniqueness validation
  - Email format and uniqueness validation
  - Password policy enforcement
  - Role assignment from available roles
  - BCrypt password hashing

#### **Read Users**
- **UI**: UserManagementView with role filtering and search
- **Service Methods**:
  - `GetAllUsersAsync()` - All users with role details
  - `GetUserByIdAsync(int id)` - Single user details
  - `GetUserByUsernameAsync(string username)` - Login authentication
  - `GetAllRolesAsync()` - Available roles for assignment
- **Include Strategy**: Always include Role information

#### **Update User**
- **UI**: UserManagementView → "Edit User" button → UserEditDialog
- **Service**: `UserService.UpdateUserAsync(User user)`
- **Validation**: Username and email uniqueness excluding current user
- **Audit**: Track user modifications
- **Password Change**: Separate method for password updates

#### **Delete User**
- **UI**: UserManagementView → "Delete User" button → Confirmation dialog
- **Service**: `UserService.DeleteUserAsync(int id)`
- **Business Rules**: Cannot delete currently logged in user
- **Soft Delete**: Sets IsActive = false
- **Data Integrity**: Preserve user references in audit logs

#### **User Management Operations**
- **Activate**: `ActivateUserAsync(int id)` - Reactivate deactivated user
- **Deactivate**: `DeactivateUserAsync(int id)` - Temporarily disable user
- **Reset Password**: `ResetPasswordAsync(int id, string newPassword)` - Admin password reset
- **Unlock Account**: Reset login attempts and lock status

---

## 📊 Audit Logs Operations

### Entity Definition
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; }
    public string Action { get; set; }
    public string EntityId { get; set; }
    public string EntityName { get; set; }
    public int? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
    public string AdditionalInfo { get; set; }
    
    // Navigation Properties
    public User User { get; set; }
}
```

### Operations (Read-Only)

#### **Create Audit Log**
- **Automatic**: Triggered by all CRUD operations on other entities
- **Service**: `AuditLogService.LogAsync(string entityType, string action, string entityId, ...)`
- **Specialized Methods**:
  - `LogCreateAsync<T>(T entity, string additionalInfo)` - Create operations
  - `LogUpdateAsync<T>(T oldEntity, T newEntity, string additionalInfo)` - Update operations
  - `LogDeleteAsync<T>(T entity, string additionalInfo)` - Delete operations
- **JSON Serialization**: Old/new values stored as JSON

#### **Read Audit Logs**
- **UI**: AuditLogsView with comprehensive filtering
- **Service Methods**:
  - `GetAllAuditLogsAsync()` - All log entries
  - `GetAuditLogsByEntityTypeAsync(string entityType)` - Filter by entity
  - `GetAuditLogsByActionAsync(string action)` - Filter by action
  - `GetAuditLogsByUserAsync(int userId)` - Filter by user
  - `GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate)` - Date range
  - `GetFilteredAuditLogsAsync(...)` - Combined filters
- **Include Strategy**: Always include User information

#### **Audit Log Analysis**
- **Entity Types**: Ship, ShipSystem, Component, Software, ChangeRequest, Document, User
- **Actions**: CREATE, UPDATE, DELETE, APPROVE, REJECT, SUBMIT
- **Search Capabilities**: Full-text search in old/new values
- **Export**: CSV export for compliance reporting

---

## 🔗 Data Relationships & Integrity

### Hierarchical Relationships
```
Ship (1) → (N) ShipSystem (1) → (N) Component (1) → (N) Software
```

### Cross-Entity Relationships
```
User (1) → (N) ChangeRequest
User (1) → (N) Document
User (1) → (N) AuditLog
Ship (1) → (N) Document
Ship (1) → (N) ChangeRequest
```

### Referential Integrity Rules
1. **Cascade Delete Prevention**: Cannot delete parent with active children
2. **Soft Delete Preference**: Preserve data relationships for audit trail
3. **Foreign Key Constraints**: Enforced at database level
4. **Orphan Prevention**: UI validation prevents orphaned records

### Data Validation Rules
- **Required Fields**: Enforced at entity and UI level
- **Uniqueness Constraints**: IMO numbers, usernames, serial numbers
- **Format Validation**: Email addresses, version numbers, dates
- **Business Rules**: Build years, port counts, file sizes

---

## 🛠️ CRUD Implementation Patterns

### Service Layer Pattern
```csharp
public class BaseService<T> where T : class
{
    protected readonly MaritimeERPContext _context;
    protected readonly ILogger<BaseService<T>> _logger;
    protected readonly IAuditLogService _auditLogService;
    
    public virtual async Task<T> CreateAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
        await _auditLogService.LogCreateAsync(entity);
        return entity;
    }
    
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }
    
    public virtual async Task<T> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
    
    public virtual async Task<T> UpdateAsync(T entity)
    {
        var oldEntity = await GetByIdAsync(entity.Id);
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await _auditLogService.LogUpdateAsync(oldEntity, entity);
        return entity;
    }
    
    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            await _auditLogService.LogDeleteAsync(entity);
            return true;
        }
        return false;
    }
}
```

### ViewModel Command Pattern
```csharp
public class BaseViewModel : INotifyPropertyChanged
{
    public ICommand CreateCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    
    protected virtual async Task CreateAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _service.CreateAsync(CurrentEntity);
            await RefreshDataAsync();
            StatusMessage = "Created successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    protected virtual async Task UpdateAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _service.UpdateAsync(CurrentEntity);
            await RefreshDataAsync();
            StatusMessage = "Updated successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    protected virtual async Task DeleteAsync()
    {
        if (MessageBox.Show("Are you sure?", "Confirm Delete", 
            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                await _service.DeleteAsync(SelectedEntity.Id);
                await RefreshDataAsync();
                StatusMessage = "Deleted successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

### Entity Framework Configuration
```csharp
public class MaritimeERPContext : DbContext
{
    public DbSet<Ship> Ships { get; set; }
    public DbSet<ShipSystem> Systems { get; set; }
    public DbSet<Component> Components { get; set; }
    public DbSet<Software> Software { get; set; }
    public DbSet<ChangeRequest> ChangeRequests { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<ShipSystem>()
            .HasOne(s => s.Ship)
            .WithMany(s => s.Systems)
            .HasForeignKey(s => s.ShipId);
            
        modelBuilder.Entity<Component>()
            .HasOne(c => c.System)
            .WithMany(s => s.Components)
            .HasForeignKey(c => c.SystemId);
            
        modelBuilder.Entity<Software>()
            .HasOne(s => s.InstalledComponent)
            .WithMany(c => c.Software)
            .HasForeignKey(s => s.InstalledComponentId);
            
        // Configure constraints
        modelBuilder.Entity<Ship>()
            .HasIndex(s => s.ImoNumber)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
```

---

## 🎯 Summary

This CRUD-focused functional design specification provides:

1. **Detailed CRUD Operations** for all eight core entities
2. **Entity Definitions** with properties and relationships
3. **Service Layer Implementation** with standardized patterns
4. **UI Interaction Flows** for each operation
5. **Data Validation Rules** and business logic
6. **Audit Trail Integration** for all operations
7. **Error Handling Patterns** and user feedback
8. **Performance Considerations** for data access

The system implements a consistent CRUD pattern across all entities while respecting the hierarchical relationships and business rules specific to maritime operations.

---

**Maritime ERP System** - *Professional Maritime Enterprise Resource Planning Solution*

🚢 *"Navigate your maritime operations with confidence"* 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MaritimeERP.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly string _documentsPath;

        // Allowed file types with their MIME types
        private static readonly Dictionary<string, string[]> AllowedFileTypes = new()
        {
            { "pdf", new[] { "application/pdf" } },
            { "doc", new[] { "application/msword" } },
            { "docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
            { "xls", new[] { "application/vnd.ms-excel" } },
            { "xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
            { "ppt", new[] { "application/vnd.ms-powerpoint" } },
            { "pptx", new[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation" } },
            { "txt", new[] { "text/plain" } },
            { "rtf", new[] { "application/rtf", "text/rtf" } },
            { "csv", new[] { "text/csv", "application/csv" } },
            { "png", new[] { "image/png" } },
            { "jpg", new[] { "image/jpeg" } },
            { "jpeg", new[] { "image/jpeg" } },
            { "gif", new[] { "image/gif" } },
            { "bmp", new[] { "image/bmp" } },
            { "tiff", new[] { "image/tiff" } },
            { "dwg", new[] { "application/dwg", "image/vnd.dwg" } },
            { "dxf", new[] { "application/dxf", "image/vnd.dxf" } }
        };

        public DocumentService(MaritimeERPContext context, ILogger<DocumentService> logger, IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
            
            // Create documents directory if it doesn't exist
            _documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
            if (!Directory.Exists(_documentsPath))
            {
                Directory.CreateDirectory(_documentsPath);
            }
        }

        #region Document CRUD Operations

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.IsActive)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                throw;
            }
        }

        public async Task<List<Document>> GetDocumentsByShipIdAsync(int shipId)
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.ShipId == shipId && d.IsActive)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for ship {ShipId}", shipId);
                throw;
            }
        }

        public async Task<List<Document>> GetDocumentsByCategoryIdAsync(int categoryId)
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.CategoryId == categoryId && d.IsActive)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for category {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Include(d => d.Versions)
                    .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with ID {DocumentId}", id);
                throw;
            }
        }

        public async Task<Document> CreateDocumentAsync(Document document, Stream fileStream, string originalFileName)
        {
            try
            {
                // Validate file
                var isValid = await ValidateFileAsync(fileStream, originalFileName, document.ContentType, document.CategoryId);
                if (!isValid)
                {
                    throw new ArgumentException("File validation failed");
                }

                // Save file to disk
                var filePath = await SaveFileAsync(fileStream, originalFileName, document.ContentType);
                
                // Set file properties
                document.FilePath = filePath;
                document.FileName = originalFileName;
                document.FileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
                document.FileSizeBytes = fileStream.Length;
                document.FileHash = GetFileHash(fileStream);
                document.UploadedAt = DateTime.UtcNow;

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentName} saved to database with ID {DocumentId}, now calling audit logging", 
                    document.Name, document.Id);

                // Create initial version
                await CreateDocumentVersionAsync(document.Id, fileStream, originalFileName, document.UploadedByUserId, "Initial version");

                // Log the creation
                _logger.LogInformation("About to call audit log for document creation: Document ID {DocumentId}, Name: {DocumentName}", 
                    document.Id, document.Name);
                
                try
                {
                    await _auditLogService.LogCreateAsync(document, "Document uploaded");
                    _logger.LogInformation("Successfully called audit log for document {DocumentId}", document.Id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log document creation audit for {DocumentId}: {AuditError}", 
                        document.Id, auditEx.Message);
                    // Continue execution - don't fail document creation because audit failed
                }

                _logger.LogInformation("Document {DocumentName} created successfully", document.Name);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document {DocumentName}", document.Name);
                throw;
            }
        }

        public async Task<Document> UpdateDocumentAsync(Document document)
        {
            try
            {
                var existingDocument = await _context.Documents.FindAsync(document.Id);
                if (existingDocument == null)
                {
                    throw new KeyNotFoundException($"Document with ID {document.Id} not found");
                }

                // Store old values for audit
                var oldDocument = new Document
                {
                    Id = existingDocument.Id,
                    Name = existingDocument.Name,
                    Description = existingDocument.Description,
                    CategoryId = existingDocument.CategoryId,
                    ShipId = existingDocument.ShipId,
                    Comments = existingDocument.Comments,
                    IsApproved = existingDocument.IsApproved,
                    IsActive = existingDocument.IsActive
                };

                // Update properties
                existingDocument.Name = document.Name;
                existingDocument.Description = document.Description;
                existingDocument.CategoryId = document.CategoryId;
                existingDocument.ShipId = document.ShipId;
                existingDocument.Comments = document.Comments;
                existingDocument.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the update
                await _auditLogService.LogUpdateAsync(oldDocument, existingDocument, "Document updated");

                _logger.LogInformation("Document {DocumentId} updated successfully", document.Id);
                return existingDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", document.Id);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    return false;
                }

                // Soft delete
                document.IsActive = false;
                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the deletion
                await _auditLogService.LogDeleteAsync(document, "Document deleted");

                _logger.LogInformation("Document {DocumentId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                throw;
            }
        }

        #endregion

        #region Document Approval

        public async Task<bool> ApproveDocumentAsync(int documentId, int approvedByUserId, string? comments = null)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    return false;
                }

                document.IsApproved = true;
                document.ApprovedByUserId = approvedByUserId;
                document.ApprovedAt = DateTime.UtcNow;
                document.UpdatedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(comments))
                {
                    document.Comments = comments;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} approved in database, now calling audit logging", documentId);

                // Log the approval
                try
                {
                    await _auditLogService.LogActionAsync("Document", "APPROVE", documentId.ToString(), 
                        document.Name, $"Document approved. Comments: {comments}");
                    _logger.LogInformation("Successfully logged approval audit for document {DocumentId}", documentId);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log document approval audit for {DocumentId}: {AuditError}", 
                        documentId, auditEx.Message);
                }

                _logger.LogInformation("Document {DocumentId} approved by user {UserId}", documentId, approvedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<bool> RejectDocumentAsync(int documentId, int rejectedByUserId, string comments)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    return false;
                }

                document.IsApproved = false;
                document.ApprovedByUserId = rejectedByUserId;
                document.ApprovedAt = DateTime.UtcNow;
                document.UpdatedAt = DateTime.UtcNow;
                document.Comments = comments;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} rejected in database, now calling audit logging", documentId);

                // Log the rejection
                try
                {
                    await _auditLogService.LogActionAsync("Document", "REJECT", documentId.ToString(), 
                        document.Name, $"Document rejected. Reason: {comments}");
                    _logger.LogInformation("Successfully logged rejection audit for document {DocumentId}", documentId);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log document rejection audit for {DocumentId}: {AuditError}", 
                        documentId, auditEx.Message);
                }

                _logger.LogInformation("Document {DocumentId} rejected by user {UserId}", documentId, rejectedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", documentId);
                throw;
            }
        }

        #endregion

        #region File Operations

        public async Task<bool> ValidateFileAsync(Stream fileStream, string fileName, string contentType, int categoryId)
        {
            try
            {
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
                
                // Check file type
                if (!await IsFileTypeAllowedAsync(fileExtension, categoryId))
                {
                    _logger.LogWarning("File type {FileExtension} not allowed for category {CategoryId}", fileExtension, categoryId);
                    return false;
                }

                // Check file size
                if (!await IsFileSizeValidAsync(fileStream.Length, categoryId))
                {
                    _logger.LogWarning("File size {FileSize} exceeds limit for category {CategoryId}", fileStream.Length, categoryId);
                    return false;
                }

                // Check MIME type
                if (AllowedFileTypes.ContainsKey(fileExtension))
                {
                    var allowedMimeTypes = AllowedFileTypes[fileExtension];
                    if (!allowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("MIME type {ContentType} not allowed for file extension {FileExtension}", contentType, fileExtension);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file {FileName}", fileName);
                return false;
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var categoryFolder = Path.Combine(_documentsPath, "uploads");
                
                if (!Directory.Exists(categoryFolder))
                {
                    Directory.CreateDirectory(categoryFolder);
                }

                var filePath = Path.Combine(categoryFolder, uniqueFileName);
                
                using var fileStream2 = new FileStream(filePath, FileMode.Create);
                fileStream.Position = 0;
                await fileStream.CopyToAsync(fileStream2);
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName}", fileName);
                throw;
            }
        }

        public async Task<Stream> GetFileStreamAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream for {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                return false;
            }
        }

        #endregion

        #region File Validation

        public async Task<bool> IsFileTypeAllowedAsync(string fileExtension, int categoryId)
        {
            try
            {
                var category = await _context.DocumentCategories.FindAsync(categoryId);
                if (category?.AllowedFileTypes == null)
                {
                    return AllowedFileTypes.ContainsKey(fileExtension.ToLowerInvariant());
                }

                var allowedTypes = category.AllowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLowerInvariant());
                
                return allowedTypes.Contains(fileExtension.ToLowerInvariant());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file type {FileExtension} for category {CategoryId}", fileExtension, categoryId);
                return false;
            }
        }

        public async Task<bool> IsFileSizeValidAsync(long fileSizeBytes, int categoryId)
        {
            try
            {
                var category = await _context.DocumentCategories.FindAsync(categoryId);
                if (category?.MaxFileSizeBytes == null)
                {
                    return fileSizeBytes <= 50 * 1024 * 1024; // Default 50MB
                }

                return fileSizeBytes <= category.MaxFileSizeBytes.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file size {FileSize} for category {CategoryId}", fileSizeBytes, categoryId);
                return false;
            }
        }

        public string GetFileHash(Stream fileStream)
        {
            try
            {
                fileStream.Position = 0;
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(fileStream);
                return Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing file hash");
                throw;
            }
        }

        #endregion

        #region Document Categories

        public async Task<List<DocumentCategory>> GetAllCategoriesAsync()
        {
            try
            {
                return await _context.DocumentCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document categories");
                throw;
            }
        }

        public async Task<List<DocumentCategory>> GetCategoriesByCategoryAsync(string category)
        {
            try
            {
                return await _context.DocumentCategories
                    .Where(c => c.Category == category && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document categories for {Category}", category);
                throw;
            }
        }

        public async Task<DocumentCategory?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _context.DocumentCategories
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document category {CategoryId}", id);
                throw;
            }
        }

        #endregion

        #region Document Versions

        public async Task<List<DocumentVersion>> GetDocumentVersionsAsync(int documentId)
        {
            try
            {
                return await _context.DocumentVersions
                    .Include(v => v.UploadedBy)
                    .Where(v => v.DocumentId == documentId && v.IsActive)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving versions for document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<DocumentVersion> CreateDocumentVersionAsync(int documentId, Stream fileStream, string fileName, int uploadedByUserId, string? changeDescription = null)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    throw new KeyNotFoundException($"Document with ID {documentId} not found");
                }

                // Get next version number
                var lastVersion = await _context.DocumentVersions
                    .Where(v => v.DocumentId == documentId)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync();

                var nextVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;

                // Save the file
                var filePath = await SaveFileAsync(fileStream, fileName, document.ContentType);

                var version = new DocumentVersion
                {
                    DocumentId = documentId,
                    VersionNumber = nextVersionNumber,
                    FileName = fileName,
                    FilePath = filePath,
                    FileSizeBytes = fileStream.Length,
                    FileHash = GetFileHash(fileStream),
                    ContentType = document.ContentType,
                    UploadedByUserId = uploadedByUserId,
                    UploadedAt = DateTime.UtcNow,
                    ChangeDescription = changeDescription,
                    IsActive = true
                };

                _context.DocumentVersions.Add(version);
                
                // Update document version
                document.Version = nextVersionNumber;
                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document version {VersionNumber} saved to database for document {DocumentId}, now calling audit logging", 
                    nextVersionNumber, documentId);

                // Log version creation
                try
                {
                    await _auditLogService.LogCreateAsync(version, $"New document version created: v{nextVersionNumber}");
                    _logger.LogInformation("Successfully logged audit for document version {VersionNumber} of document {DocumentId}", 
                        nextVersionNumber, documentId);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log document version creation audit for version {VersionNumber} of document {DocumentId}: {AuditError}", 
                        nextVersionNumber, documentId, auditEx.Message);
                }

                _logger.LogInformation("Version {VersionNumber} created for document {DocumentId}", nextVersionNumber, documentId);
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version for document {DocumentId}", documentId);
                throw;
            }
        }

        #endregion

        #region Search and Filtering

        public async Task<List<Document>> SearchDocumentsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllDocumentsAsync();
                }

                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.IsActive && (
                        d.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        d.Description!.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        d.FileName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        d.Category.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        d.Ship!.ShipName.ToLowerInvariant().Contains(lowerSearchTerm)
                    ))
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<List<Document>> GetDocumentsForApprovalAsync()
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Where(d => d.IsActive && !d.IsApproved)
                    .OrderBy(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for approval");
                throw;
            }
        }

        public async Task<List<Document>> GetRecentDocumentsAsync(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await _context.Documents
                    .Include(d => d.Category)
                    .Include(d => d.Ship)
                    .Include(d => d.UploadedBy)
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.IsActive && d.UploadedAt >= cutoffDate)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent documents");
                throw;
            }
        }

        #endregion

        #region Statistics

        public async Task<Dictionary<string, int>> GetDocumentStatsByCategory()
        {
            try
            {
                return await _context.Documents
                    .Include(d => d.Category)
                    .Where(d => d.IsActive)
                    .GroupBy(d => d.Category.Name)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document statistics by category");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetDocumentStatsByStatus()
        {
            try
            {
                return await _context.Documents
                    .Where(d => d.IsActive)
                    .GroupBy(d => d.IsApproved ? "Approved" : "Pending Approval")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document statistics by status");
                throw;
            }
        }

        public async Task<int> GetTotalDocumentCount()
        {
            try
            {
                return await _context.Documents.CountAsync(d => d.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total document count");
                throw;
            }
        }

        public async Task<long> GetTotalFileSize()
        {
            try
            {
                return await _context.Documents
                    .Where(d => d.IsActive)
                    .SumAsync(d => d.FileSizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total file size");
                throw;
            }
        }

        #endregion

        #region Preview and Download

        public async Task<byte[]> GetDocumentPreviewAsync(int documentId)
        {
            try
            {
                var document = await GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new KeyNotFoundException($"Document with ID {documentId} not found");
                }

                // For now, return the full content. In a real implementation,
                // you might generate thumbnails or extract text for preview
                return await GetDocumentContentAsync(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document preview {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<byte[]> GetDocumentContentAsync(int documentId)
        {
            try
            {
                var document = await GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new KeyNotFoundException($"Document with ID {documentId} not found");
                }

                if (!File.Exists(document.FilePath))
                {
                    throw new FileNotFoundException($"File not found: {document.FilePath}");
                }

                return await File.ReadAllBytesAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document content {DocumentId}", documentId);
                throw;
            }
        }

        public string GetDocumentDownloadUrl(int documentId)
        {
            return $"/api/documents/{documentId}/download";
        }

        #endregion

        #region Audit Logging Test

        /// <summary>
        /// Test method to verify audit logging is working for documents
        /// </summary>
        public async Task<bool> TestAuditLoggingAsync()
        {
            try
            {
                _logger.LogInformation("Starting audit logging test for DocumentService");
                
                // Test simple action logging
                await _auditLogService.LogActionAsync("Document", "TEST", "test-id", "Test Document", "Audit logging test");
                
                _logger.LogInformation("Audit logging test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit logging test failed: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        #endregion
    }
} 
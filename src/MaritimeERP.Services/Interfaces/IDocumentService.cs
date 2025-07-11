using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IDocumentService
    {
        // Document CRUD operations
        Task<List<Document>> GetAllDocumentsAsync();
        Task<List<Document>> GetDocumentsByShipIdAsync(int shipId);
        Task<List<Document>> GetDocumentsByCategoryIdAsync(int categoryId);
        Task<Document?> GetDocumentByIdAsync(int id);
        Task<Document> CreateDocumentAsync(Document document, Stream fileStream, string originalFileName);
        Task<Document> UpdateDocumentAsync(Document document);
        Task<bool> DeleteDocumentAsync(int id);

        // Document approval
        Task<bool> ApproveDocumentAsync(int documentId, int approvedByUserId, string? comments = null);
        Task<bool> RejectDocumentAsync(int documentId, int rejectedByUserId, string comments);

        // File operations
        Task<bool> ValidateFileAsync(Stream fileStream, string fileName, string contentType, int categoryId);
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileStreamAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        
        // File validation
        Task<bool> IsFileTypeAllowedAsync(string fileExtension, int categoryId);
        Task<bool> IsFileSizeValidAsync(long fileSizeBytes, int categoryId);
        string GetFileHash(Stream fileStream);

        // Document categories
        Task<List<DocumentCategory>> GetAllCategoriesAsync();
        Task<List<DocumentCategory>> GetCategoriesByCategoryAsync(string category);
        Task<DocumentCategory?> GetCategoryByIdAsync(int id);

        // Document versions
        Task<List<DocumentVersion>> GetDocumentVersionsAsync(int documentId);
        Task<DocumentVersion> CreateDocumentVersionAsync(int documentId, Stream fileStream, string fileName, int uploadedByUserId, string? changeDescription = null);

        // Search and filtering
        Task<List<Document>> SearchDocumentsAsync(string searchTerm);
        Task<List<Document>> GetDocumentsForApprovalAsync();
        Task<List<Document>> GetRecentDocumentsAsync(int days = 30);

        // Statistics
        Task<Dictionary<string, int>> GetDocumentStatsByCategory();
        Task<Dictionary<string, int>> GetDocumentStatsByStatus();
        Task<int> GetTotalDocumentCount();
        Task<long> GetTotalFileSize();

        // Preview and download
        Task<byte[]> GetDocumentPreviewAsync(int documentId);
        Task<byte[]> GetDocumentContentAsync(int documentId);
        string GetDocumentDownloadUrl(int documentId);
    }
} 
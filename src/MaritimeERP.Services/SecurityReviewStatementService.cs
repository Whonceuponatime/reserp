using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaritimeERP.Services
{
    public class SecurityReviewStatementService : ISecurityReviewStatementService
    {
        private readonly MaritimeERPContext _context;

        public SecurityReviewStatementService(MaritimeERPContext context)
        {
            _context = context;
        }

        public async Task<List<SecurityReviewStatement>> GetAllSecurityReviewStatementsAsync()
        {
            return await _context.SecurityReviewStatements
                .Include(s => s.User)
                .Include(s => s.Ship)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<SecurityReviewStatement?> GetSecurityReviewStatementByIdAsync(int id)
        {
            return await _context.SecurityReviewStatements
                .Include(s => s.User)
                .Include(s => s.Ship)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SecurityReviewStatement?> GetSecurityReviewStatementByRequestNumberAsync(string requestNumber)
        {
            return await _context.SecurityReviewStatements
                .Include(s => s.User)
                .Include(s => s.Ship)
                .FirstOrDefaultAsync(s => s.RequestNumber == requestNumber);
        }

        public async Task<SecurityReviewStatement> CreateSecurityReviewStatementAsync(SecurityReviewStatement securityReviewStatement)
        {
            if (string.IsNullOrEmpty(securityReviewStatement.RequestNumber))
            {
                securityReviewStatement.RequestNumber = await GenerateRequestNumberAsync();
            }

            securityReviewStatement.CreatedDate = DateTime.Now;
            securityReviewStatement.UpdatedDate = DateTime.Now;

            _context.SecurityReviewStatements.Add(securityReviewStatement);
            await _context.SaveChangesAsync();
            return securityReviewStatement;
        }

        public async Task<SecurityReviewStatement> UpdateSecurityReviewStatementAsync(SecurityReviewStatement securityReviewStatement)
        {
            // Get the existing entity from the database to avoid tracking conflicts
            var existingStatement = await _context.SecurityReviewStatements.FindAsync(securityReviewStatement.Id);
            if (existingStatement == null)
            {
                throw new InvalidOperationException($"SecurityReviewStatement with ID {securityReviewStatement.Id} not found");
            }

            // Update the properties of the existing tracked entity
            existingStatement.RequestNumber = securityReviewStatement.RequestNumber;
            existingStatement.ReviewerDepartment = securityReviewStatement.ReviewerDepartment;
            existingStatement.ReviewerPosition = securityReviewStatement.ReviewerPosition;
            existingStatement.ReviewerName = securityReviewStatement.ReviewerName;
            existingStatement.ReviewDate = securityReviewStatement.ReviewDate;
            existingStatement.ReviewItem1 = securityReviewStatement.ReviewItem1;
            existingStatement.ReviewResult1 = securityReviewStatement.ReviewResult1;
            existingStatement.ReviewRemarks1 = securityReviewStatement.ReviewRemarks1;
            existingStatement.ReviewItem2 = securityReviewStatement.ReviewItem2;
            existingStatement.ReviewResult2 = securityReviewStatement.ReviewResult2;
            existingStatement.ReviewRemarks2 = securityReviewStatement.ReviewRemarks2;
            existingStatement.ReviewItem3 = securityReviewStatement.ReviewItem3;
            existingStatement.ReviewResult3 = securityReviewStatement.ReviewResult3;
            existingStatement.ReviewRemarks3 = securityReviewStatement.ReviewRemarks3;
            existingStatement.ReviewItem4 = securityReviewStatement.ReviewItem4;
            existingStatement.ReviewResult4 = securityReviewStatement.ReviewResult4;
            existingStatement.ReviewRemarks4 = securityReviewStatement.ReviewRemarks4;
            existingStatement.ReviewItem5 = securityReviewStatement.ReviewItem5;
            existingStatement.ReviewResult5 = securityReviewStatement.ReviewResult5;
            existingStatement.ReviewRemarks5 = securityReviewStatement.ReviewRemarks5;
            existingStatement.OverallReviewResult = securityReviewStatement.OverallReviewResult;
            existingStatement.ReviewOpinion = securityReviewStatement.ReviewOpinion;
            existingStatement.IsUnderReview = securityReviewStatement.IsUnderReview;
            existingStatement.IsApproved = securityReviewStatement.IsApproved;
            existingStatement.ShipId = securityReviewStatement.ShipId;
            existingStatement.UserId = securityReviewStatement.UserId;
            existingStatement.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return existingStatement;
        }

        public async Task<bool> DeleteSecurityReviewStatementAsync(int id)
        {
            var securityReviewStatement = await _context.SecurityReviewStatements.FindAsync(id);
            if (securityReviewStatement == null)
                return false;

            _context.SecurityReviewStatements.Remove(securityReviewStatement);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"SER-{today:yyyyMM}";
            
            var lastRequest = await _context.SecurityReviewStatements
                .Where(s => s.RequestNumber.StartsWith(prefix))
                .OrderByDescending(s => s.RequestNumber)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
            {
                return $"{prefix}-001";
            }

            var lastNumberPart = lastRequest.RequestNumber.Split('-').Last();
            if (int.TryParse(lastNumberPart, out int lastNumber))
            {
                return $"{prefix}-{(lastNumber + 1):D3}";
            }

            return $"{prefix}-001";
        }

        public async Task<SecurityReviewStatement> SubmitForReviewAsync(int id, int reviewerId)
        {
            var statement = await _context.SecurityReviewStatements.FindAsync(id);
            if (statement == null)
                throw new InvalidOperationException("Security review statement not found");

            statement.IsUnderReview = true;
            statement.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return statement;
        }

        public async Task<SecurityReviewStatement> ApproveAsync(int id, int approverId)
        {
            var statement = await _context.SecurityReviewStatements.FindAsync(id);
            if (statement == null)
                throw new InvalidOperationException("Security review statement not found");

            statement.IsApproved = true;
            statement.IsUnderReview = false;
            statement.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return statement;
        }

        public async Task<SecurityReviewStatement> RejectAsync(int id, int rejectorId, string reason)
        {
            var statement = await _context.SecurityReviewStatements.FindAsync(id);
            if (statement == null)
                throw new InvalidOperationException("Security review statement not found");

            statement.IsUnderReview = false;
            statement.IsApproved = false;
            statement.ReviewOpinion = reason;
            statement.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return statement;
        }

        public async Task<List<SecurityReviewStatement>> GetByReviewerAsync(int userId)
        {
            return await _context.SecurityReviewStatements
                .Include(s => s.User)
                .Include(s => s.Ship)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SecurityReviewStatement>> GetPendingReviewAsync()
        {
            return await _context.SecurityReviewStatements
                .Include(s => s.User)
                .Include(s => s.Ship)
                .Where(s => s.IsUnderReview && !s.IsApproved)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SecurityReviewStatement>> GetByStatusAsync(string status)
        {
            return status.ToLower() switch
            {
                "draft" => await _context.SecurityReviewStatements
                    .Include(s => s.User)
                    .Include(s => s.Ship)
                    .Where(s => !s.IsUnderReview && !s.IsApproved)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync(),
                "review" => await GetPendingReviewAsync(),
                "approved" => await _context.SecurityReviewStatements
                    .Include(s => s.User)
                    .Include(s => s.Ship)
                    .Where(s => s.IsApproved)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync(),
                _ => await GetAllSecurityReviewStatementsAsync()
            };
        }
    }
} 
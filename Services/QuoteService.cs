using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InsuranceClaimsAPI.Services
{
    public interface IQuoteService
    {
        Task<Quote> SubmitAsync(
            Quote quote,
            int? uploadedById = null,
            IEnumerable<QuoteAttachmentRequest>? attachments = null,
            CancellationToken cancellationToken = default);
        Task SetStatusAsync(int quoteId, QuoteStatus status, string? reasonOrNotes = null);
        Task<IReadOnlyList<Quote>> GetByClaimAsync(int claimId);
        Task<IReadOnlyList<Quote>> GetAllAsync();
        Task<Quote?> GetByIdAsync(int quoteId);
        Task<IReadOnlyList<Quote>> GetByProviderFirebaseIdAsync(string firebaseUid);
        Task<IReadOnlyList<Quote>> GetForInsurerAsync(int insurerId);
        Task<bool> DeleteAsync(int quoteId);
        Task<IReadOnlyList<QuoteDocument>> AddDocumentsAsync(
            int quoteId,
            int uploadedById,
            IEnumerable<QuoteAttachmentRequest> attachments,
            CancellationToken cancellationToken = default);
        Task<byte[]> GenerateInvoicePdfAsync(int quoteId);
    }

    public class QuoteService : IQuoteService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<QuoteService> _logger;

        public QuoteService(
            InsuranceClaimsContext context, 
            IAuditService auditService,
            INotificationService notificationService,
            ILogger<QuoteService> logger)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Quote> SubmitAsync(
            Quote quote,
            int? uploadedById = null,
            IEnumerable<QuoteAttachmentRequest>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                quote.Status = QuoteStatus.Submitted;
                quote.DateSubmitted = DateTime.UtcNow;

                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync(new AuditLog
                {
                    Action = AuditAction.Create,
                    EntityType = EntityType.Quote,
                    EntityId = quote.QuoteId.ToString(),
                    ActionDescription = "Quote submitted"
                });

                if (attachments != null && attachments.Any() && uploadedById.HasValue)
                {
                    var uploadedDocuments = await AddDocumentsAsync(quote.QuoteId, uploadedById.Value, attachments, cancellationToken);
                    quote.QuoteDocuments = uploadedDocuments.ToList();
                }

                // Load claim with insurer to get insurer ID for notification
                var claim = await _context.Claims
                    .Include(c => c.Insurer)
                    .FirstOrDefaultAsync(c => c.Id == quote.PolicyId, cancellationToken);

                if (claim != null && claim.InsurerId > 0)
                {
                    // Notify the insurer that a new quote has been submitted
                    await _notificationService.CreateAsync(new Notification
                    {
                        UserId = claim.InsurerId,
                        QuoteId = quote.QuoteId,
                        Message = $"A new quote for ${quote.Amount:N2} has been submitted for claim {claim.ClaimNumber}",
                        DateSent = DateTime.UtcNow,
                        Status = NotificationStatus.Unread
                    });
                }

                await transaction.CommitAsync(cancellationToken);
                return quote;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task SetStatusAsync(int quoteId, QuoteStatus status, string? reasonOrNotes = null)
        {
            var quote = await _context.Quotes
                .Include(q => q.Provider)
                .Include(q => q.Policy)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            
            if (quote == null) return;

            var oldStatus = quote.Status;
            quote.Status = status;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.QuoteStatusChange,
                EntityType = EntityType.Quote,
                EntityId = quoteId.ToString(),
                ActionDescription = $"Quote status changed from {oldStatus} to {status}"
            });

            // Notify the provider when quote status changes
            if (quote.ProviderId > 0 && oldStatus != status)
            {
                var statusMessage = status switch
                {
                    QuoteStatus.Approved => "approved",
                    QuoteStatus.Rejected => "rejected",
                    QuoteStatus.Revised => "requires revision",
                    _ => "updated"
                };

                var notificationMessage = $"Your quote for ${quote.Amount:N2} has been {statusMessage}";
                if (!string.IsNullOrEmpty(reasonOrNotes))
                {
                    notificationMessage += $": {reasonOrNotes}";
                }

                await _notificationService.CreateAsync(new Notification
                {
                    UserId = quote.ProviderId,
                    QuoteId = quote.QuoteId,
                    Message = notificationMessage,
                    DateSent = DateTime.UtcNow,
                    Status = NotificationStatus.Unread
                });
            }
        }

        public async Task<IReadOnlyList<Quote>> GetByClaimAsync(int claimId)
        {
            return await _context.Quotes
                .Where(q => q.PolicyId == claimId)
                .Include(q => q.QuoteDocuments)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Quote>> GetAllAsync()
        {
            return await _context.Quotes
                .Include(q => q.Policy)
                .Include(q => q.QuoteDocuments)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<Quote?> GetByIdAsync(int quoteId)
        {
            return await _context.Quotes
                .Include(q => q.Policy)
                .Include(q => q.Provider)
                .Include(q => q.QuoteDocuments)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
        }

        public async Task<IReadOnlyList<Quote>> GetByProviderFirebaseIdAsync(string firebaseUid)
        {
            var provider = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid && u.Role == UserRole.Provider);

            if (provider == null)
            {
                return new List<Quote>();
            }

            return await _context.Quotes
                .Include(q => q.Policy)
                .Include(q => q.QuoteDocuments)
                .Where(q => q.ProviderId == provider.Id)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Quote>> GetForInsurerAsync(int insurerId)
        {
            return await _context.Quotes
                .Include(q => q.Policy)
                    .ThenInclude(c => c.Provider)
                .Include(q => q.QuoteDocuments)
                .Where(q => q.Policy != null && q.Policy.InsurerId == insurerId)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.Policy)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                return false;
            }

            // Log audit before deletion
            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Delete,
                EntityType = EntityType.Quote,
                EntityId = quoteId.ToString(),
                ActionDescription = $"Quote for ${quote.Amount:N2} deleted"
            });

            // Note: Notifications with QuoteId will be set to NULL automatically due to ON DELETE SET NULL constraint
            // QuoteDocuments will be cascade deleted

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IReadOnlyList<QuoteDocument>> AddDocumentsAsync(
            int quoteId,
            int uploadedById,
            IEnumerable<QuoteAttachmentRequest> attachments,
            CancellationToken cancellationToken = default)
        {
            if (attachments == null)
            {
                return Array.Empty<QuoteDocument>();
            }

            var attachmentList = attachments
                .Where(a => a != null && (
                    !string.IsNullOrWhiteSpace(a.ContentBase64) ||
                    !string.IsNullOrWhiteSpace(a.Url) ||
                    !string.IsNullOrWhiteSpace(a.StoragePath)))
                .ToList();

            if (attachmentList.Count == 0)
            {
                return Array.Empty<QuoteDocument>();
            }

            var quoteExists = await _context.Quotes
                .AnyAsync(q => q.QuoteId == quoteId, cancellationToken);

            if (!quoteExists)
            {
                throw new InvalidOperationException($"Quote with ID {quoteId} was not found");
            }

            var relativeRoot = Path.Combine("uploads", "quotes", quoteId.ToString());
            var physicalRoot = Path.Combine(Directory.GetCurrentDirectory(), relativeRoot);
            Directory.CreateDirectory(physicalRoot);

            var savedDocuments = new List<QuoteDocument>();
            foreach (var attachment in attachmentList)
            {
                var sanitizedFileName = EnsureFileName(attachment.FileName, attachment.MimeType);

                if (HasInlineContent(attachment))
                {
                    var document = await SaveBase64AttachmentAsync(
                        quoteId,
                        uploadedById,
                        attachment,
                        sanitizedFileName,
                        relativeRoot,
                        physicalRoot,
                        cancellationToken);

                    savedDocuments.Add(document);
                    continue;
                }

                var remoteDocument = BuildRemoteAttachment(
                    quoteId,
                    uploadedById,
                    attachment,
                    sanitizedFileName);

                if (remoteDocument != null)
                {
                    savedDocuments.Add(remoteDocument);
                }
            }

            if (savedDocuments.Count == 0)
            {
                return Array.Empty<QuoteDocument>();
            }

            await _context.QuoteDocuments.AddRangeAsync(savedDocuments, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var document in savedDocuments)
            {
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = uploadedById,
                    Action = AuditAction.DocumentUploaded,
                    EntityType = EntityType.Document,
                    EntityId = document.Id.ToString(),
                    ActionDescription = $"Document '{document.FileName}' uploaded for quote {quoteId}"
                });
            }

            return savedDocuments;
        }

        private static string EnsureFileName(string fileName, string? mimeType)
        {
            var sanitizedFileName = SanitizeFileName(fileName);
            if (string.IsNullOrWhiteSpace(sanitizedFileName))
            {
                sanitizedFileName = $"quote-document-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            }

            var extension = Path.GetExtension(sanitizedFileName);
            if (string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(mimeType))
            {
                var inferredExtension = MimeMapping.TryGetExtension(mimeType);
                if (!string.IsNullOrWhiteSpace(inferredExtension))
                {
                    sanitizedFileName += inferredExtension.StartsWith('.') ? inferredExtension : $".{inferredExtension}";
                }
            }

            return sanitizedFileName;
        }

        private static bool HasInlineContent(QuoteAttachmentRequest attachment)
            => !string.IsNullOrWhiteSpace(attachment.ContentBase64);

        private async Task<QuoteDocument> SaveBase64AttachmentAsync(
            int quoteId,
            int uploadedById,
            QuoteAttachmentRequest attachment,
            string sanitizedFileName,
            string relativeRoot,
            string physicalRoot,
            CancellationToken cancellationToken)
        {
            try
            {
                var decodedBytes = DecodeBase64(attachment.ContentBase64 ?? string.Empty);
                if (decodedBytes.Length == 0)
                {
                    throw new InvalidOperationException("Attachment content is empty after decoding");
                }

                var uniqueFileName = GetUniqueFileName(physicalRoot, sanitizedFileName);
                var relativePath = Path.Combine(relativeRoot, uniqueFileName).Replace("\\", "/");
                var physicalPath = Path.Combine(physicalRoot, uniqueFileName);

                await File.WriteAllBytesAsync(physicalPath, decodedBytes, cancellationToken);

                var document = CreateDocument(
                    quoteId,
                    uploadedById,
                    uniqueFileName,
                    relativePath,
                    attachment);

                document.FileSizeBytes = attachment.FileSizeBytes ?? decodedBytes.LongLength;
                document.CreatedAt = DateTime.UtcNow;

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload attachment '{FileName}' for quote {QuoteId}", attachment.FileName, quoteId);
                throw new InvalidOperationException($"Failed to process attachment '{attachment.FileName}'. {ex.Message}");
            }
        }

        private QuoteDocument? BuildRemoteAttachment(
            int quoteId,
            int uploadedById,
            QuoteAttachmentRequest attachment,
            string sanitizedFileName)
        {
            var filePath = attachment.StoragePath ?? attachment.Url;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning(
                    "Skipping attachment '{FileName}' for quote {QuoteId} because no content or URL was provided",
                    attachment.FileName,
                    quoteId);
                return null;
            }

            var document = CreateDocument(
                quoteId,
                uploadedById,
                sanitizedFileName,
                filePath,
                attachment);

            document.FileSizeBytes = attachment.FileSizeBytes ?? 0;
            document.CreatedAt = attachment.UploadedAt ?? DateTime.UtcNow;

            return document;
        }

        private QuoteDocument CreateDocument(
            int quoteId,
            int uploadedById,
            string fileName,
            string filePath,
            QuoteAttachmentRequest attachment)
        {
            return new QuoteDocument
            {
                QuoteId = quoteId,
                UploadedById = uploadedById,
                FileName = fileName,
                FilePath = filePath,
                FileExtension = Path.GetExtension(fileName),
                MimeType = string.IsNullOrWhiteSpace(attachment.MimeType) ? "application/octet-stream" : attachment.MimeType,
                FileSizeBytes = attachment.FileSizeBytes ?? 0,
                Type = attachment.DocumentType ?? QuoteDocumentType.Other,
                Title = attachment.Title,
                Description = attachment.Description,
                Tags = attachment.Tags,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static byte[] DecodeBase64(string content)
        {
            var data = content?.Trim() ?? string.Empty;
            var commaIndex = data.IndexOf(',');
            if (commaIndex >= 0)
            {
                data = data[(commaIndex + 1)..];
            }

            return Convert.FromBase64String(data);
        }

        private static string SanitizeFileName(string fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty);
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }
            return name;
        }

        private static string GetUniqueFileName(string directory, string fileName)
        {
            var uniqueName = fileName;
            var extension = Path.GetExtension(fileName);
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var counter = 1;

            while (File.Exists(Path.Combine(directory, uniqueName)))
            {
                uniqueName = $"{baseName}_{counter}{extension}";
                counter++;
            }

            return uniqueName;
        }

        private static class MimeMapping
        {
            private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
            {
                { "application/pdf", ".pdf" },
                { "image/jpeg", ".jpg" },
                { "image/png", ".png" },
                { "image/gif", ".gif" },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
                { "application/msword", ".doc" },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
                { "application/vnd.ms-excel", ".xls" },
                { "text/plain", ".txt" }
            };

            public static string? TryGetExtension(string? mimeType)
            {
                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    return null;
                }

                return MimeToExtension.TryGetValue(mimeType, out var extension) ? extension : null;
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.Policy)
                    .ThenInclude(c => c.Provider)
                .Include(q => q.Policy)
                    .ThenInclude(c => c.Insurer)
                .Include(q => q.Provider)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                throw new InvalidOperationException($"Quote with ID {quoteId} was not found");
            }

            if (quote.Policy == null)
            {
                throw new InvalidOperationException($"Claim associated with quote {quoteId} was not found");
            }

            var claim = quote.Policy;
            var provider = quote.Provider ?? claim.Provider;
            var insurer = claim.Insurer;

            // Generate PDF using QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
            
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Quote #: {quote.QuoteId}").FontSize(12);
                                column.Item().Text($"Date: {quote.DateSubmitted:MMMM dd, yyyy}").FontSize(12);
                            });

                            row.ConstantItem(100).AlignRight().Column(column =>
                            {
                                column.Item().Text("Status:").FontSize(10);
                                column.Item().Text(quote.Status.ToString()).FontSize(12).Bold().FontColor(GetStatusColor(quote.Status));
                            });
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(1, Unit.Centimetre);

                            // Company Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("FROM:").FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                                    col.Item().Text(provider?.CompanyName ?? $"{provider?.FirstName} {provider?.LastName}").FontSize(12).Bold();
                                    if (!string.IsNullOrWhiteSpace(provider?.Address))
                                    {
                                        col.Item().Text(provider.Address).FontSize(10);
                                        if (!string.IsNullOrWhiteSpace(provider.City))
                                        {
                                            col.Item().Text($"{provider.City}, {provider.PostalCode} {provider.Country}").FontSize(10);
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(provider?.Email))
                                    {
                                        col.Item().Text($"Email: {provider.Email}").FontSize(10);
                                    }
                                    if (!string.IsNullOrWhiteSpace(provider?.PhoneNumber))
                                    {
                                        col.Item().Text($"Phone: {provider.PhoneNumber}").FontSize(10);
                                    }
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("TO:").FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                                    col.Item().Text(insurer?.CompanyName ?? $"{insurer?.FirstName} {insurer?.LastName}").FontSize(12).Bold();
                                    if (!string.IsNullOrWhiteSpace(insurer?.Address))
                                    {
                                        col.Item().Text(insurer.Address).FontSize(10);
                                        if (!string.IsNullOrWhiteSpace(insurer.City))
                                        {
                                            col.Item().Text($"{insurer.City}, {insurer.PostalCode} {insurer.Country}").FontSize(10);
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(insurer?.Email))
                                    {
                                        col.Item().Text($"Email: {insurer.Email}").FontSize(10);
                                    }
                                    if (!string.IsNullOrWhiteSpace(insurer?.PhoneNumber))
                                    {
                                        col.Item().Text($"Phone: {insurer.PhoneNumber}").FontSize(10);
                                    }
                                });
                            });

                            column.Item().PaddingTop(0.5f, Unit.Centimetre).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // Claim Details Section
                            column.Item().Text("CLAIM DETAILS").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().PaddingTop(0.3f, Unit.Centimetre).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Claim Number: {claim.ClaimNumber}").FontSize(11);
                                    col.Item().Text($"Title: {claim.Title}").FontSize(11);
                                    col.Item().Text($"Policy Number: {claim.PolicyNumber ?? "N/A"}").FontSize(11);
                                    col.Item().Text($"Policy Holder: {claim.PolicyHolderName ?? "N/A"}").FontSize(11);
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Status: {claim.Status}").FontSize(11);
                                    col.Item().Text($"Priority: {claim.Priority}").FontSize(11);
                                    if (claim.IncidentDate.HasValue)
                                    {
                                        col.Item().Text($"Incident Date: {claim.IncidentDate.Value:MMMM dd, yyyy}").FontSize(11);
                                    }
                                    if (claim.IncidentLocation != null)
                                    {
                                        col.Item().Text($"Location: {claim.IncidentLocation}").FontSize(11);
                                    }
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(claim.Description))
                            {
                                column.Item().PaddingTop(0.3f, Unit.Centimetre).Text("Description:").FontSize(11).Bold();
                                column.Item().Text(claim.Description).FontSize(10).LineHeight(1.4f);
                            }

                            column.Item().PaddingTop(0.5f, Unit.Centimetre).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // Quote/Invoice Details
                            column.Item().Text("INVOICE DETAILS").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().PaddingTop(0.3f, Unit.Centimetre)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(120);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Item").Bold();
                                        header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .PaddingVertical(5)
                                                .PaddingHorizontal(5);
                                        }
                                    });

                                    table.Cell().Element(CellStyle).Text($"Quote for Claim #{claim.ClaimNumber}");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"${quote.Amount:N2}");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten1)
                                            .PaddingVertical(8)
                                            .PaddingHorizontal(5);
                                    }
                                });

                            // Summary Section
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).AlignRight().Column(summary =>
                            {
                                summary.Item().Width(200).Row(row =>
                                {
                                    row.RelativeItem().Text("Subtotal:").FontSize(11);
                                    row.ConstantItem(100).AlignRight().Text($"${quote.Amount:N2}").FontSize(11);
                                });

                                summary.Item().Width(200).Row(row =>
                                {
                                    row.RelativeItem().Text("Total Amount:").FontSize(12).Bold();
                                    row.ConstantItem(100).AlignRight().Text($"${quote.Amount:N2}").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                                });
                            });

                            column.Item().PaddingTop(1, Unit.Centimetre).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // Additional Information
                            if (!string.IsNullOrWhiteSpace(claim.Notes))
                            {
                                column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("Notes:").FontSize(11).Bold();
                                column.Item().Text(claim.Notes).FontSize(10).LineHeight(1.4f);
                            }

                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text($"Estimated Amount: ${claim.EstimatedAmount:N2}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            if (claim.ApprovedAmount > 0)
                            {
                                column.Item().Text($"Approved Amount: ${claim.ApprovedAmount:N2}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.Span($"{DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm:ss UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                });
            })
            .GeneratePdf();

            return pdfBytes;
        }

        private static string GetStatusColor(QuoteStatus status)
        {
            return status switch
            {
                QuoteStatus.Approved => Colors.Green.Darken2,
                QuoteStatus.Rejected => Colors.Red.Darken2,
                QuoteStatus.Revised => Colors.Orange.Darken2,
                QuoteStatus.Submitted => Colors.Blue.Darken2,
                _ => Colors.Grey.Darken2
            };
        }
    }
}



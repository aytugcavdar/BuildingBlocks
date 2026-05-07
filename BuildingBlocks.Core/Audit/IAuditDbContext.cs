using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Core.Audit;

/// <summary>
/// EfCore DbContext'lerin AuditLog tablosuna sahip olduğunu belirten ara yüz.
/// Interceptor'lar bu arayüze ulaşarak ilgili değişiklikleri kaydedecek.
/// </summary>
public interface IAuditDbContext
{
    DbSet<AuditLog> AuditLogs { get; }
}

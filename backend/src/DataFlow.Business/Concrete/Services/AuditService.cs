using DataFlow.Business.Abstract;
using DataFlow.DataAccess.Context;
using DataFlow.DataAccess.Entities;

namespace DataFlow.Business.Concrete.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(int? userId, string username, string action, string? detail = null, string? ip = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Detail = detail,
            IpAddress = ip
        });

        await _db.SaveChangesAsync();
    }
}

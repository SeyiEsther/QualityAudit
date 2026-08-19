using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;

namespace QualityAudit.Services;

/// <summary>
/// Resolves the current user. There is no authentication yet (same as the TL Portal at
/// this stage), so the identity source is deliberately pluggable: today it reads an
/// 'X-Username' header. When Windows Auth is added later, only this class changes.
/// </summary>
public class UserContext
{
    private readonly IHttpContextAccessor _http;
    private readonly QualityAuditContext _db;

    public UserContext(IHttpContextAccessor http, QualityAuditContext db)
    {
        _http = http;
        _db = db;
    }

    public string? CurrentUsername =>
        _http.HttpContext?.Request.Headers["X-Username"].FirstOrDefault();

    /// <summary>
    /// True if the caller may use Admin. With no identity supplied we allow it (no auth yet),
    /// so the QE team isn't locked out before Windows Auth lands. Once a username IS supplied,
    /// it is enforced against AuditUsers.IsAdmin.
    /// </summary>
    public async Task<bool> IsAdminAsync()
    {
        var username = CurrentUsername;
        if (string.IsNullOrWhiteSpace(username))
            return true; // pluggable seam: no auth configured yet

        var user = await _db.AuditUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IsActive && u.Username == username);

        return user is { IsAdmin: true };
    }
}

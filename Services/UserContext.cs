using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;

namespace QualityAudit.Services;

public class UserContext
{
    private readonly IHttpContextAccessor _http;
    private readonly QualityAuditContext _db;

    public UserContext(IHttpContextAccessor http, QualityAuditContext db)
    {
        _http = http;
        _db = db;
    }

    public string? CurrentUser =>
        _http.HttpContext?.Request.Headers["X-Username"].FirstOrDefault();

    public async Task<bool> IsAdminAsync()
    {
        var who = CurrentUser;
        if (string.IsNullOrWhiteSpace(who))
            return true;

        var user = await _db.AuditUsers.AsNoTracking().FirstOrDefaultAsync(u =>
            u.IsActive && (u.Username == who || u.Email == who || u.DisplayName == who));

        return user is { IsAdmin: true };
    }
}

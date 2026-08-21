using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
public class AttachmentsController : ControllerBase
{
    private const long MaxBytes = 10 * 1024 * 1024; // ~10 MB

    private readonly QualityAuditContext _db;
    private readonly AttachmentStorage _storage;

    public AttachmentsController(QualityAuditContext db, AttachmentStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Multipart image upload for a result. Files go on disk; metadata to the DB.</summary>
    [HttpPost("api/results/{resultId:int}/attachments")]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(int resultId, [FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });
        if (file.Length > MaxBytes)
            return BadRequest(new { error = "File is too large (max 10 MB)." });
        if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only image files are allowed." });

        var exists = await _db.Results.AnyAsync(r => r.Id == resultId);
        if (!exists) return NotFound(new { error = $"Result {resultId} not found." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8) ext = ".img";

        string storedName;
        long size;
        await using (var stream = file.OpenReadStream())
            (storedName, size) = await _storage.SaveAsync(stream, ext);

        var attachment = new ResultAttachment
        {
            ResultId = resultId,
            FileName = Path.GetFileName(file.FileName),
            StoredPath = storedName,
            ContentType = file.ContentType,
            SizeBytes = size,
            UploadedAt = DateTime.UtcNow
        };
        _db.ResultAttachments.Add(attachment);
        await _db.SaveChangesAsync();

        return Ok(new AttachmentDto { Id = attachment.Id, FileName = attachment.FileName });
    }

    /// <summary>Lists the attachments (id + original name) for a result — used for thumbnails.</summary>
    [HttpGet("api/results/{resultId:int}/attachments")]
    public async Task<IEnumerable<AttachmentDto>> List(int resultId) =>
        await _db.ResultAttachments.AsNoTracking()
            .Where(a => a.ResultId == resultId)
            .OrderBy(a => a.Id)
            .Select(a => new AttachmentDto { Id = a.Id, FileName = a.FileName })
            .ToListAsync();

    /// <summary>Streams an attachment's bytes back with its stored content type.</summary>
    [HttpGet("api/attachments/{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var att = await _db.ResultAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (att is null) return NotFound();
        if (!_storage.Exists(att.StoredPath)) return NotFound();

        var stream = System.IO.File.OpenRead(_storage.FullPath(att.StoredPath));
        return File(stream, att.ContentType ?? "application/octet-stream");
    }
}

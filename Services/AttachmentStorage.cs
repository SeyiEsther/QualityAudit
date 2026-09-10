namespace QualityAudit.Services;

public class AttachmentStorage
{
    private readonly string _root;

    public AttachmentStorage(IConfiguration config)
    {
        _root = config["Storage:AttachmentRoot"]
                ?? Path.Combine(AppContext.BaseDirectory, "attachments");
        Directory.CreateDirectory(_root);
    }

    public async Task<(string StoredName, long Size)> SaveAsync(Stream content, string extension)
    {
        var storedName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(_root, storedName);
        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs);
        }
        return (storedName, new FileInfo(fullPath).Length);
    }

    public string FullPath(string storedName) => Path.Combine(_root, storedName);

    public bool Exists(string storedName) => File.Exists(FullPath(storedName));
}

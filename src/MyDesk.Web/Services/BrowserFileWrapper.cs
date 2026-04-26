using Microsoft.AspNetCore.Components.Forms;

namespace MyDesk.Web.Services;

/// <summary>
/// Wraps IFormFile as IBrowserFile for use with BrandAssetService
/// </summary>
public class BrowserFileWrapper : IBrowserFile
{
    private readonly Stream _stream;

    public BrowserFileWrapper(string name, string contentType, long size, Stream stream)
    {
        Name = name;
        ContentType = contentType;
        Size = size;
        _stream = stream;
    }

    public string Name { get; }
    public string ContentType { get; }
    public long Size { get; }
    public DateTimeOffset LastModified { get; }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return _stream;
    }
}

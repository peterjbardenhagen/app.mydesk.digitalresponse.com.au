namespace MyDesk.Shared;

public interface IBrowserFile
{
    string Name { get; }
    long Size { get; }
    string ContentType { get; }
    DateTimeOffset LastModified { get; }
    Stream OpenReadStream(long maxAllowedSize = 512000);
}

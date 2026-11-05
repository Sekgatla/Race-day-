namespace RaceDay.Api.Storage;

public sealed class LocalImageStorage : IImageStorage
{
    private readonly string _root;
    private readonly string _rootWithSeparator;

    public LocalImageStorage(string root)
    {
        _root = Path.GetFullPath(root);
        _rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_root);
    }

    public async Task StoreAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
    }

    public async Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var content = await File.ReadAllBytesAsync(path, cancellationToken);
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return new StoredImage(content, contentType);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("The image key is invalid.", nameof(key));
        }

        var relativePath = key.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!path.StartsWith(_rootWithSeparator, StringComparison.Ordinal) &&
            !string.Equals(path, _root, StringComparison.Ordinal))
        {
            throw new ArgumentException("The image key is outside the storage directory.", nameof(key));
        }

        return path;
    }
}

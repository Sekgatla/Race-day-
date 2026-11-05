namespace RaceDay.Api.Storage;

public sealed record StoredImage(byte[] Content, string ContentType);

public interface IImageStorage
{
    Task StoreAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

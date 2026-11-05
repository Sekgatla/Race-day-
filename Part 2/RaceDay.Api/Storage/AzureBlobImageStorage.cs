using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Storage;

public sealed class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobImageStorage(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task StoreAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = _container.GetBlobClient(key);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);
    }

    public async Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(key);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blob.DownloadContentAsync(cancellationToken);
        return new StoredImage(
            response.Value.Content.ToArray(),
            response.Value.Details.ContentType ?? "application/octet-stream");
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}

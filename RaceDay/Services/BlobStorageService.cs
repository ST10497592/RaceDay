using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Services;

// =======================================================
// START: AZURE BLOB STORAGE SERVICE
// Purpose: Stores uploaded images (event banners and profile
// pictures) in Azure Blob Storage. Only the API talks to Azure;
// the MVC application sends files to the API.
// =======================================================

/// <summary>Settings bound from the "AzureStorage" configuration section.</summary>
public class BlobStorageOptions
{
    /// <summary>Storage account connection string (user secrets / environment variable, never committed).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Container that holds all uploaded images.</summary>
    public string ContainerName { get; set; } = "raceday-images";

    /// <summary>
    /// Optional public base URL used to build the URLs returned to browsers
    /// (needed with the Azurite emulator, where the API reaches Azurite on a
    /// different host name than the browser does). Leave empty for real Azure.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}

/// <summary>Raised when files cannot be stored (not configured or storage unreachable).</summary>
public class StorageUnavailableException : Exception
{
    public StorageUnavailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public interface IBlobStorageService
{
    /// <summary>Uploads an already validated image and returns its public URL.</summary>
    Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private BlobContainerClient? _container;

    public BlobStorageService(
        IOptions<BlobStorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new StorageUnavailableException(
                "File storage is not configured on the server.");
        }

        try
        {
            var container = await GetContainerAsync(cancellationToken);

            var extension = ImageUploadValidator.GetNormalisedExtension(file);
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
            var blob = container.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = ImageUploadValidator.GetContentType(extension)
                }
            };

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, options, cancellationToken);

            return BuildUrl(blob, blobName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage request failed.");
            throw new StorageUnavailableException(
                "The file storage service is currently unavailable.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage could not be reached.");
            throw new StorageUnavailableException(
                "The file storage service is currently unavailable.", ex);
        }
    }

    // START: CONTAINER INITIALISATION
    private async Task<BlobContainerClient> GetContainerAsync(
        CancellationToken cancellationToken)
    {
        if (_container != null)
            return _container;

        await _initLock.WaitAsync(cancellationToken);

        try
        {
            if (_container != null)
                return _container;

            var clientOptions = new BlobClientOptions();
            clientOptions.Retry.MaxRetries = 2;
            clientOptions.Retry.Delay = TimeSpan.FromSeconds(1);
            clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(20);

            var serviceClient = new BlobServiceClient(
                _options.ConnectionString,
                clientOptions);

            var container = serviceClient.GetBlobContainerClient(
                _options.ContainerName);

            try
            {
                // Blob-level public read so browsers can display the images.
                await container.CreateIfNotExistsAsync(
                    PublicAccessType.Blob,
                    cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == "PublicAccessNotPermitted")
            {
                _logger.LogWarning(
                    "The storage account does not allow public blob access. " +
                    "The container was created as private.");

                await container.CreateIfNotExistsAsync(
                    cancellationToken: cancellationToken);
            }

            _container = container;
            return container;
        }
        finally
        {
            _initLock.Release();
        }
    }
    // END: CONTAINER INITIALISATION

    private string BuildUrl(BlobClient blob, string blobName)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return _options.PublicBaseUrl.TrimEnd('/') + "/" +
                   _options.ContainerName + "/" + blobName;
        }

        return blob.Uri.ToString();
    }
}

// =======================================================
// END: AZURE BLOB STORAGE SERVICE
// =======================================================

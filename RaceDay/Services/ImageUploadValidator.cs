namespace RaceDay.Services;

// =======================================================
// START: IMAGE UPLOAD VALIDATOR
// Purpose: Server-side validation of uploaded images
// (size, extension and file signature).
// =======================================================

public static class ImageUploadValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

    /// <summary>Returns an error message when the file is not an acceptable image, otherwise null.</summary>
    public static async Task<string?> ValidateAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Please choose an image file to upload.";

        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrEmpty(extension) || !ContentTypes.ContainsKey(extension))
            return "Only JPG, PNG, GIF and WEBP images are allowed.";

        var header = new byte[12];
        int read;

        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAsync(header, 0, header.Length);
        }

        if (!HasImageSignature(header, read))
            return "The uploaded file is not a valid image.";

        return null;
    }

    public static string GetNormalisedExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension == ".jpeg" ? ".jpg" : extension;
    }

    public static string GetContentType(string extension)
    {
        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static bool HasImageSignature(byte[] h, int length)
    {
        if (length < 12)
            return false;

        // JPEG
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
            return true;

        // PNG
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47)
            return true;

        // GIF
        if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38)
            return true;

        // WEBP: "RIFF" .... "WEBP"
        if (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
            h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50)
            return true;

        return false;
    }
}

// =======================================================
// END: IMAGE UPLOAD VALIDATOR
// =======================================================

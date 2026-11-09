using Microsoft.AspNetCore.Http;
using RaceDay.Services;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: IMAGE UPLOAD VALIDATOR TESTS
// Unit tests for the server-side image validation used before
// anything is sent to Azure Blob Storage.
// =======================================================

public class ImageUploadValidatorTests
{
    private static IFormFile CreateFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    [Fact]
    public async Task ValidPng_IsAccepted()
    {
        var file = CreateFile(TestHelpers.PngBytes, "photo.png");

        Assert.Null(await ImageUploadValidator.ValidateAsync(file));
    }

    [Fact]
    public async Task ValidJpeg_IsAccepted()
    {
        var jpeg = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
            0x49, 0x46, 0x00, 0x01, 0x01, 0x00
        };

        Assert.Null(await ImageUploadValidator.ValidateAsync(CreateFile(jpeg, "photo.jpg")));
    }

    [Fact]
    public async Task NullFile_IsRejected()
    {
        Assert.NotNull(await ImageUploadValidator.ValidateAsync(null));
    }

    [Fact]
    public async Task EmptyFile_IsRejected()
    {
        var file = CreateFile(Array.Empty<byte>(), "empty.png");

        Assert.NotNull(await ImageUploadValidator.ValidateAsync(file));
    }

    [Fact]
    public async Task UnsupportedExtension_IsRejected()
    {
        var file = CreateFile(TestHelpers.PngBytes, "document.pdf");

        Assert.NotNull(await ImageUploadValidator.ValidateAsync(file));
    }

    [Fact]
    public async Task ImageExtensionWithNonImageContent_IsRejected()
    {
        var text = System.Text.Encoding.UTF8.GetBytes("<script>alert('hi')</script>");

        Assert.NotNull(await ImageUploadValidator.ValidateAsync(CreateFile(text, "evil.png")));
    }

    [Fact]
    public async Task FileLargerThanFiveMegabytes_IsRejected()
    {
        var big = new byte[ImageUploadValidator.MaxBytes + 1];
        Array.Copy(TestHelpers.PngBytes, big, TestHelpers.PngBytes.Length);

        Assert.NotNull(await ImageUploadValidator.ValidateAsync(CreateFile(big, "huge.png")));
    }

    [Theory]
    [InlineData("photo.JPEG", ".jpg")]
    [InlineData("photo.PNG", ".png")]
    [InlineData("photo.webp", ".webp")]
    public void Extension_IsNormalised(string fileName, string expected)
    {
        var file = CreateFile(TestHelpers.PngBytes, fileName);

        Assert.Equal(expected, ImageUploadValidator.GetNormalisedExtension(file));
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".webp", "image/webp")]
    public void ContentType_IsDerivedFromTheExtension(string extension, string expected)
    {
        Assert.Equal(expected, ImageUploadValidator.GetContentType(extension));
    }
}

// =======================================================
// END: IMAGE UPLOAD VALIDATOR TESTS
// =======================================================

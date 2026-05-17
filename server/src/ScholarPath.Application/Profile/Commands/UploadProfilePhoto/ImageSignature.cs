namespace ScholarPath.Application.Profile.Commands.UploadProfilePhoto;

/// <summary>
/// Verifies an uploaded file's leading bytes (its "magic number" / file
/// signature) genuinely match a JPEG, PNG or WebP image — so a non-image file
/// renamed with an image extension is rejected before it is stored.
/// </summary>
public static class ImageSignature
{
    // JPEG: FF D8 FF
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];

    // PNG: 89 50 4E 47 0D 0A 1A 0A
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // WebP is a RIFF container: bytes 0-3 are "RIFF", bytes 8-11 are "WEBP".
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="content"/> begins with
    /// a JPEG, PNG or WebP signature. The stream is read from its current
    /// position; callers should rewind it first and afterwards as needed.
    /// </summary>
    public static bool IsRecognizedImage(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // 12 bytes is enough to cover the longest signature check (WebP).
        var header = new byte[12];
        var read = ReadFully(content, header);
        return IsRecognizedImage(header.AsSpan(0, read));
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="header"/> (the leading
    /// bytes of a file) starts with a JPEG, PNG or WebP signature.
    /// </summary>
    public static bool IsRecognizedImage(ReadOnlySpan<byte> header)
        => StartsWith(header, Jpeg)
        || StartsWith(header, Png)
        || IsWebp(header);

    private static bool IsWebp(ReadOnlySpan<byte> header)
        => header.Length >= 12
        && StartsWith(header, Riff)
        && header.Slice(8, 4).SequenceEqual(Webp);

    private static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> signature)
        => header.Length >= signature.Length
        && header[..signature.Length].SequenceEqual(signature);

    /// <summary>Reads up to <c>buffer.Length</c> bytes, tolerating short reads.</summary>
    private static int ReadFully(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }
}

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Outcome of an antivirus scan of an uploaded file.
/// </summary>
public enum FileScanVerdict
{
    /// <summary>The scanner inspected the file and found no malware.</summary>
    Clean = 0,

    /// <summary>The scanner identified the file as malware.</summary>
    Infected = 1,

    /// <summary>
    /// The scan could not be completed — the scanner was unreachable, timed
    /// out, or returned an error. Callers must treat this as a failure and
    /// reject the upload (fail-closed); an unscanned file is never stored.
    /// </summary>
    ScanUnavailable = 2,
}

/// <summary>
/// Result of scanning a single file. <see cref="Detail"/> carries the malware
/// signature name when <see cref="Verdict"/> is <see cref="FileScanVerdict.Infected"/>,
/// or a short failure reason when it is <see cref="FileScanVerdict.ScanUnavailable"/>.
/// </summary>
public sealed record FileScanResult(FileScanVerdict Verdict, string? Detail)
{
    /// <summary>True only when the scanner positively confirmed the file is clean.</summary>
    public bool IsClean => Verdict == FileScanVerdict.Clean;
}

/// <summary>
/// Antivirus scanning of uploaded file content (SRS security NFR). Every
/// upload path scans the raw bytes through this service <b>before</b> the file
/// is persisted to storage, and rejects the upload unless the verdict is
/// <see cref="FileScanVerdict.Clean"/> (fail-closed).
/// </summary>
public interface IFileScanService
{
    /// <summary>
    /// Scans <paramref name="content"/> for malware. The stream position is
    /// reset before and after the scan so the caller can still read the bytes
    /// to store them when the verdict is clean.
    /// </summary>
    Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken ct);
}

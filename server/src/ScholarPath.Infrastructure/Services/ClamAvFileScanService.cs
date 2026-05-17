using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nClam;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real <see cref="IFileScanService"/> backed by a ClamAV <c>clamd</c> daemon.
/// Uploaded bytes are streamed to the daemon via INSTREAM (nClam's
/// <c>SendAndScanFileAsync</c>) before they are persisted. Registered in DI
/// when <c>FileScanning:Enabled</c> is true.
/// </summary>
/// <remarks>
/// Any failure to reach or get a definite answer from the daemon maps to
/// <see cref="FileScanVerdict.ScanUnavailable"/>; the upload pipeline rejects
/// that outcome, so an unscanned file is never stored (fail-closed).
/// </remarks>
public sealed class ClamAvFileScanService(
    IOptions<FileScanningOptions> options,
    ILogger<ClamAvFileScanService> logger) : IFileScanService
{
    private readonly FileScanningOptions _opts = options.Value;

    public async Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Scan from the start of the stream regardless of where the caller left it.
        if (content.CanSeek)
            content.Position = 0;

        try
        {
            // ClamClient is a thin, cheap wrapper over a per-scan TCP connection;
            // creating one per scan (rather than holding a shared instance) keeps
            // it stateless and thread-safe. It is not IDisposable.
            var client = new ClamClient(_opts.ClamAvHost, _opts.ClamAvPort);
            var result = await client.SendAndScanFileAsync(content, ct).ConfigureAwait(false);

            switch (result.Result)
            {
                case ClamScanResults.Clean:
                    logger.LogInformation("[clamav] {FileName} scanned clean.", fileName);
                    return new FileScanResult(FileScanVerdict.Clean, null);

                case ClamScanResults.VirusDetected:
                    var signature = result.InfectedFiles?.FirstOrDefault()?.VirusName ?? "unknown";
                    logger.LogWarning(
                        "[clamav] {FileName} REJECTED — malware detected: {Signature}",
                        fileName, signature);
                    return new FileScanResult(FileScanVerdict.Infected, signature);

                case ClamScanResults.Error:
                case ClamScanResults.Unknown:
                default:
                    logger.LogError(
                        "[clamav] {FileName} scan returned {Result}; raw='{Raw}'. Treating as unavailable.",
                        fileName, result.Result, result.RawResult);
                    return new FileScanResult(
                        FileScanVerdict.ScanUnavailable,
                        $"Scanner returned {result.Result}.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancelled — propagate, do not mask as a scan failure.
            throw;
        }
#pragma warning disable CA1031 // any failure to reach clamd must fail closed, not crash the upload
        catch (Exception ex)
        {
            // Unreachable daemon, socket timeout, protocol error — fail closed.
            logger.LogError(ex,
                "[clamav] {FileName} could not be scanned ({Host}:{Port}). Upload will be rejected.",
                fileName, _opts.ClamAvHost, _opts.ClamAvPort);
            return new FileScanResult(
                FileScanVerdict.ScanUnavailable,
                "ClamAV daemon unreachable or scan failed.");
        }
#pragma warning restore CA1031
        finally
        {
            // Rewind so the caller can still read the bytes to store them.
            if (content.CanSeek)
                content.Position = 0;
        }
    }
}

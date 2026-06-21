namespace HamBusLog.Services;

using System.Diagnostics;
using System.Text;
using HamBusLog.Models;
using HamBusLog.Wa1gonLib.Adif;

/// <summary>
/// Uploads QSOs to ARRL Logbook of the World via the tqsl command-line signing tool.
/// </summary>
public sealed class LotwUploadService
{
    private static readonly TimeSpan TqslTimeout = TimeSpan.FromMinutes(3);

    public static LotwUploadService CreateDefault()
    {
        var config = AppConfigurationStore.Load();
        return new LotwUploadService(config.Lotw, AppConfigurationStore.GetActiveProfile(config));
    }

    private readonly LotwConfiguration _config;
    private readonly ConfigProfile _profile;

    public LotwUploadService(LotwConfiguration config, ConfigProfile profile)
    {
        _config = config ?? new LotwConfiguration();
        _profile = profile;
    }

    public bool IsConfigured
        => _config.Enabled
           && !string.IsNullOrWhiteSpace(ResolveStationCallsign())
           && !string.IsNullOrWhiteSpace(_config.TqslPath);

    public string StatusDescription
    {
        get
        {
            if (!_config.Enabled) return "LoTW is disabled.";
            if (string.IsNullOrWhiteSpace(ResolveStationCallsign())) return "Station callsign not configured.";
            if (string.IsNullOrWhiteSpace(_config.TqslPath)) return "TQSL path not configured.";
            return $"Ready (callsign: {ResolveStationCallsign()})";
        }
    }

    public async Task<LotwDownloadResult> DownloadAdifAsync(
        string outputPath,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return LotwDownloadResult.Fail("Output file path is required.");

        var callsign = ResolveStationCallsign();
        if (string.IsNullOrWhiteSpace(callsign))
            return LotwDownloadResult.Fail("Station callsign is not configured.");

        var tqslPath = _config.TqslPath.Trim();
        if (string.IsNullOrWhiteSpace(tqslPath))
            return LotwDownloadResult.Fail("TQSL path is not configured.");

        var downloadTemplate = string.IsNullOrWhiteSpace(_config.DownloadArgumentsTemplate)
            ? "-d -c \"{callsign}\" -o \"{output}\""
            : _config.DownloadArgumentsTemplate;

        try
        {
            progress?.Report("Calling TQSL to download LoTW QSOs...");
            var result = await RunTqslAsync(
                tqslPath,
                RenderTemplate(downloadTemplate, callsign, outputPath, from, to),
                progress,
                cancellationToken,
                operationDescription: "download");
            return result is { Success: true }
                ? LotwDownloadResult.Ok($"Downloaded LoTW ADIF to {outputPath}.")
                : LotwDownloadResult.Fail(result.Message);
        }
        catch (OperationCanceledException)
        {
            return LotwDownloadResult.Fail("Download was cancelled.");
        }
        catch (Exception ex)
        {
            return LotwDownloadResult.Fail($"Download failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Uploads a list of QSOs to LoTW using TQSL to sign and transmit.
    /// </summary>
    public async Task<LotwUploadResult> UploadQsosAsync(
        IReadOnlyList<Qso> qsos,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (qsos is null || qsos.Count == 0)
            return LotwUploadResult.Fail("No QSOs to upload.");

        var callsign = ResolveStationCallsign();
        if (string.IsNullOrWhiteSpace(callsign))
            return LotwUploadResult.Fail("Station callsign is not configured.");

        var tqslPath = _config.TqslPath.Trim();
        if (string.IsNullOrWhiteSpace(tqslPath))
            return LotwUploadResult.Fail("TQSL path is not configured.");

        // Write to a temp ADIF file
        var tempDir = Path.GetTempPath();
        var tempAdif = Path.Combine(tempDir, $"hambuslog-lotw-{Guid.NewGuid():N}.adi");

        try
        {
            progress?.Report($"Writing {qsos.Count} QSO(s) to temp ADIF...");
            var adif = AdifWriter.WriteToAdif(qsos);
            await File.WriteAllTextAsync(tempAdif, adif, Encoding.UTF8, cancellationToken);

            progress?.Report("Calling TQSL to sign and upload...");
            var result = await RunTqslAsync(tqslPath, RenderUploadArguments(callsign, tempAdif), progress, cancellationToken, operationDescription: "upload");
            return result;
        }
        catch (OperationCanceledException)
        {
            return LotwUploadResult.Fail("Upload was cancelled.");
        }
        catch (Exception ex)
        {
            return LotwUploadResult.Fail($"Upload failed: {ex.Message}");
        }
        finally
        {
            try { File.Delete(tempAdif); } catch { }
        }
    }

    public static string RenderTemplate(string template, string callsign, string outputPath, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var fromText = from?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        var toText = to?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

        return (template ?? string.Empty)
            .Replace("{callsign}", callsign ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{output}", outputPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{from}", fromText, StringComparison.OrdinalIgnoreCase)
            .Replace("{to}", toText, StringComparison.OrdinalIgnoreCase);
    }

    private static string RenderUploadArguments(string callsign, string adifPath)
        => $"-d -u -c \"{callsign}\" \"{adifPath}\"";

    private async Task<LotwUploadResult> RunTqslAsync(
        string tqslPath,
        string arguments,
        IProgress<string>? progress,
        CancellationToken cancellationToken,
        string operationDescription)
    {
        var args = new StringBuilder(arguments ?? string.Empty);

        var password = WeakSecretProtector.Decrypt(_config.PasswordCiphertext);
        if (!string.IsNullOrWhiteSpace(password) && operationDescription == "upload")
            args.Append($" -p \"{password}\"");

        var startInfo = new ProcessStartInfo
        {
            FileName = tqslPath,
            Arguments = args.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return LotwUploadResult.Fail(
                $"Could not start TQSL at '{tqslPath}'. Verify TQSL is installed and the path is correct. Detail: {ex.Message}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var waitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TqslTimeout, cancellationToken);
        var completed = await Task.WhenAny(waitTask, timeoutTask);
        if (completed != waitTask)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore kill failures; timeout error below still explains failure.
            }

            return LotwUploadResult.Fail(
                $"TQSL did not finish within {TqslTimeout.TotalMinutes:0} minutes during {operationDescription}. "
                + "Verify the command arguments and that TQSL is not waiting for interactive input.");
        }

        // Observe task completion/cancellation exceptions now that waitTask won the race.
        await waitTask;

        var exitCode = process.ExitCode;
        var output = (stdout.ToString() + " " + stderr.ToString()).Trim();

        if (exitCode == 0)
        {
            progress?.Report($"LoTW {operationDescription} complete.");
            return LotwUploadResult.Ok($"{char.ToUpperInvariant(operationDescription[0])}{operationDescription[1..]} completed successfully.");
        }

        // Exit code 8 means "no QSOs uploaded" (e.g. all dupes on LoTW side) but is not fatal.
        if (operationDescription == "upload" && exitCode == 8)
            return LotwUploadResult.Ok("Upload complete — TQSL reported no new QSOs (may all be duplicates on LoTW).");

        var detail = string.IsNullOrWhiteSpace(output) ? $"Exit code {exitCode}." : output;
        return LotwUploadResult.Fail($"TQSL exited with code {exitCode} during {operationDescription}. {detail}");
    }

    private string ResolveStationCallsign()
    {
        if (!string.IsNullOrWhiteSpace(_config.StationCallsign))
            return _config.StationCallsign.Trim().ToUpperInvariant();

        return (_profile.StationCallSign ?? string.Empty).Trim().ToUpperInvariant();
    }
}

public readonly record struct LotwUploadResult(bool Success, string Message)
{
    public static LotwUploadResult Ok(string message) => new(true, message);
    public static LotwUploadResult Fail(string message) => new(false, message);
}

public readonly record struct LotwDownloadResult(bool Success, string Message)
{
    public static LotwDownloadResult Ok(string message) => new(true, message);
    public static LotwDownloadResult Fail(string message) => new(false, message);
}





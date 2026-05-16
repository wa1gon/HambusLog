namespace HamBusLog.Services;

using System.Net.Sockets;
using System.Text;

public sealed class DxClusterSpotPublisher : IDxClusterSpotPublisher
{
    public async Task<string> SendSpotAsync(DxSpotRequest request, CancellationToken ct = default)
    {
        if (request is null)
            return "Spot request is missing.";

        var config = AppConfigurationStore.Load();
        var cluster = config.Cluster ?? new ClusterConfig();
        var host = string.IsNullOrWhiteSpace(cluster.Hostname) ? "127.0.0.1" : cluster.Hostname.Trim();
        var port = cluster.TcpPort <= 0 ? 7300 : cluster.TcpPort;

        var spotter = string.IsNullOrWhiteSpace(request.SpotterCall)
            ? (cluster.Callsign ?? string.Empty).Trim()
            : request.SpotterCall.Trim();

        if (string.IsNullOrWhiteSpace(spotter))
            return "Set a DX cluster callsign in Configuration before spotting.";

        var target = (request.TargetCall ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(target))
            return "Enter a callsign to spot.";

        if (request.FrequencyMhz <= 0)
            return "Enter a valid frequency in MHz to spot.";

        var frequencyKHz = request.FrequencyMhz * 1000m;
        var freqText = frequencyKHz.ToString("0.0", CultureInfo.InvariantCulture);
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? "HamBusLog" : request.Comment.Trim();
        var spotLine = $"DX de {spotter} {freqText} {target} {comment}";

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);

            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

            var callsign = (cluster.Callsign ?? string.Empty).Trim();
            var password = cluster.Password ?? string.Empty;
            var command = (cluster.Command ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(callsign))
                await writer.WriteLineAsync(callsign);
            if (!string.IsNullOrWhiteSpace(password))
                await writer.WriteLineAsync(password);
            if (!string.IsNullOrWhiteSpace(command))
                await writer.WriteLineAsync(command);

            await writer.WriteLineAsync(spotLine);

            _ = reader.ReadLineAsync(ct);
            return request.IsSelfSpot
                ? $"Self-spot sent for {target} at {freqText} kHz."
                : $"Spot sent for {target} at {freqText} kHz.";
        }
        catch (OperationCanceledException)
        {
            return "Spotting canceled.";
        }
        catch (SocketException ex)
        {
            return $"DX cluster connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"DX cluster spot failed: {ex.Message}";
        }
    }
}


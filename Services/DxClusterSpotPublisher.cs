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

        var target = (request.TargetCall ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(target))
            return "Enter a callsign to spot.";

        if (request.FrequencyMhz <= 0)
            return "Enter a valid frequency in MHz to spot.";

        var frequencyKHz = request.FrequencyMhz * 1000m;
        var freqText = frequencyKHz.ToString("0.0", CultureInfo.InvariantCulture);
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? string.Empty : request.Comment.Trim();
        var spotLine = string.IsNullOrWhiteSpace(comment)
            ? $"DX {target} {freqText}"
            : $"DX {target} {freqText} {comment}";

        try
        {
            App.LogDxClusterNonSpot("OUT", spotLine);

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);

            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

            await writer.WriteLineAsync(spotLine);

            var responseTask = reader.ReadLineAsync(ct).AsTask();
            var completed = await Task.WhenAny(responseTask, Task.Delay(TimeSpan.FromSeconds(2), ct));
            if (completed == responseTask)
            {
                var response = await responseTask;
                if (!string.IsNullOrWhiteSpace(response))
                    App.LogDxClusterNonSpot("IN", $"Spot response: {response}");
            }

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

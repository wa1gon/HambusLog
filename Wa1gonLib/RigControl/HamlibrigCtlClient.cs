namespace HamBusLog.Wa1gonLib.RigControl;


public class HamLibRigCtlClient(string _host, int _port) : IDisposable, IRigControlClient
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public long Freq { get; set; }
    public string Mode { get; set; } = "USB"; // Default mode

    public void Dispose()
    {
        Close();
    }

    public async Task OpenAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
        }
        catch (Exception ex)
        {
            throw new IOException("Unable to connect to rigctld.", ex);
        }
    }

    public void Close()
    {
        _stream?.Dispose();
        _client?.Close();
        _client = null;
        _stream = null;
    }

    public async Task SetFreqAsync(long freq)
    {
        EnsureConnected();
        await SendCommandAsync($"F {freq}\n");
        await EnsureCommandSucceededAsync("set frequency");
        Freq = freq;
    }

    public async Task SetModeAsync(string mode)
    {
        EnsureConnected();
        var trimmedMode = mode.Trim().ToUpperInvariant();

        // Hamlib expects mode + passband; 0 asks backend to choose a default passband.
        await SendCommandAsync($"M {trimmedMode} 0\n");
        await EnsureCommandSucceededAsync("set mode");

        // Some backends acknowledge DIGU/DIGL but keep reporting USB/LSB.
        // Verify once and retry with packet aliases when needed.
        if (trimmedMode is "DIGU" or "DIGL")
        {
            try
            {
                var (readback, _) = await GetModeAndPassbandAsync();
                var normalizedReadback = NormalizeModeToken(readback);

                if (!string.Equals(normalizedReadback, trimmedMode, StringComparison.OrdinalIgnoreCase))
                {
                    var aliasMode = trimmedMode == "DIGU" ? "PKTUSB" : "PKTLSB";
                    await SendCommandAsync($"M {aliasMode} 0\n");
                    await EnsureCommandSucceededAsync("set mode alias");
                }
            }
            catch (Exception)
            {
            }
        }

        Mode = trimmedMode;
    }

    private async Task EnsureCommandSucceededAsync(string operation)
    {
        var seenLines = new List<string>();

        // Some backends may emit an informational line before RPRT.
        // Read a few lines until we find the canonical RPRT status.
        for (var i = 0; i < 4; i++)
        {
            var response = await ReadLineAsync();
            if (string.IsNullOrWhiteSpace(response))
                continue;

            seenLines.Add(response);
            if (!response.StartsWith("RPRT", StringComparison.OrdinalIgnoreCase))
                continue;

            var codeToken = response.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
            if (!int.TryParse(codeToken, out var code) || code != 0)
                throw new IOException($"rigctld rejected {operation} (RPRT {codeToken ?? "?"}).");

            return;
        }

        if (seenLines.Count == 0)
            throw new IOException($"rigctld returned an empty reply while trying to {operation}.");

        throw new IOException($"Unexpected rigctld reply while trying to {operation}: '{string.Join(" | ", seenLines)}'.");
    }

    public async Task SendCommandAsync(string command)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to rigctld.");

        var buffer = Encoding.ASCII.GetBytes(command);
        try
        {
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            throw new IOException("Lost connection to rigctld.", ex);
        }
    }

    public async Task<RigCapabilities> GetCapabilitiesAsync()
    {
        EnsureConnected();
        await SendCommandAsync("\\dump_caps\n");

        var buffer = new byte[4096];
        var ms = new MemoryStream();

        while (true)
        {
            var bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                break; // End of stream or no more data
            ms.Write(buffer, 0, bytesRead);
            if (bytesRead < buffer.Length)
                break; // Assume end of response
        }

        var response = Encoding.ASCII.GetString(ms.ToArray());
        var lines = response.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        return RigCapabilities.Parse(lines.ToImmutableArray());
    }

    public async Task<string> ReadLineAsync()
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to rigctld.");

        var ms = new MemoryStream();
        var buffer = new byte[1];
        while (true)
        {
            var bytesRead = await _stream.ReadAsync(buffer, 0, 1);
            if (bytesRead == 0)
                throw new IOException("Lost connection to rigctld (connection closed by remote).");
            if (buffer[0] == '\n')
                break;
            if (buffer[0] == '\r')
                continue; // Ignore carriage return
            ms.WriteByte(buffer[0]);
        }

        return Encoding.ASCII.GetString(ms.ToArray()).Trim();
    }

    public async Task<string> GetModeAsync()
    {
        var (mode, _) = await GetModeAndPassbandAsync();
        return mode;
    }

    /// <summary>
    /// Returns the mode and passband width from rigctld.
    /// Returns ("", 0) when the radio reports no active slice (mode "0" or passband 0).
    /// </summary>
    public async Task<(string Mode, int Passband)> GetModeAndPassbandAsync()
    {
        EnsureConnected();
        await SendCommandAsync("m\n");

        // rigctld returns two lines for the 'm' command: first is the mode, second is the passband width.
        var modeLine = await ReadLineAsync();
        var passbandLine = await ReadLineAsync();

        if (string.IsNullOrWhiteSpace(modeLine))
            throw new IOException("Failed to parse mode from rigctld response.");

        var modeToken = modeLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(modeToken) || string.Equals(modeToken, "RPRT", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Failed to parse mode from rigctld response.");

        // "0" means the backend has no active slice / unknown mode
        if (modeToken == "0")
            return (string.Empty, 0);

        modeToken = NormalizeModeToken(modeToken);

        int.TryParse(passbandLine?.Trim(), out var passband);
        return (modeToken, passband);
    }

    private static string NormalizeModeToken(string modeToken)
    {
        return modeToken.Trim().ToUpperInvariant() switch
        {
            "PKTUSB" => "DIGU",
            "PKTLSB" => "DIGL",
            var x => x
        };
    }

    public async Task<long> GetFreqAsync()
    {
        EnsureConnected();
        await SendCommandAsync("f\n");

        var response = await ReadLineAsync();
        if (long.TryParse(response, out var freq))
            return freq;

        // Some backends include extra tokens; keep first numeric token.
        var token = response.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => long.TryParse(x, out _));
        if (!string.IsNullOrWhiteSpace(token) && long.TryParse(token, out freq))
            return freq;

        throw new IOException("Failed to parse frequency from rigctld response.");
    }

    private void EnsureConnected()
    {
        // TcpClient.Connected is not a reliable liveness signal between I/O calls.
        // We only require a created client/stream; real transport loss is detected on read/write.
        if (_client == null || _stream == null)
            throw new InvalidOperationException("Not connected to rigctld.");
    }
}

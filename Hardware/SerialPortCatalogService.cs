namespace HamBusLog.Hardware;

public interface ISerialPortCatalogService
{
    IReadOnlyList<string> GetAvailablePorts();
}

public sealed class SerialPortCatalogService : ISerialPortCatalogService
{
    private const string LinuxByIdDirectory = "/dev/serial/by-id";
    private const string FlexCatDirectory = "/tmp";
    private const string FlexCatPattern = "ttyCAT*";

    public IReadOnlyList<string> GetAvailablePorts()
    {
        var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var port in System.IO.Ports.SerialPort.GetPortNames())
                ports.Add(port);
        }
        catch
        {
            // Keep the UI responsive even when port discovery fails on the host.
        }

        if (OperatingSystem.IsLinux() && Directory.Exists(LinuxByIdDirectory))
        {
            try
            {
                foreach (var byIdPath in Directory.GetFiles(LinuxByIdDirectory))
                    ports.Add(byIdPath);
            }
            catch
            {
                // Ignore by-id lookup errors; fallback ports are still useful.
            }
        }

        if (OperatingSystem.IsLinux() && Directory.Exists(FlexCatDirectory))
        {
            try
            {
                foreach (var flexCatPath in Directory.GetFiles(FlexCatDirectory, FlexCatPattern))
                    ports.Add(flexCatPath);
            }
            catch
            {
                // Ignore /tmp scan errors; other sources still provide usable ports.
            }
        }

        return ports
            .OrderBy(path => path.StartsWith(LinuxByIdDirectory, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}



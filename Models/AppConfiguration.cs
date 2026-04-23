namespace HamBusLog.Models;

public sealed class AppConfiguration
{
    public string BackgroundColor { get; set; } = "#1F2937";
    public string ForegroundColor { get; set; } = "#FFFFFF";
    public string ConnectionString { get; set; } = "Data Source=hambuslog.db";
    public RigctldConfiguration Rigctld { get; set; } = new();
}

public sealed class RigctldConfiguration
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4532;
}


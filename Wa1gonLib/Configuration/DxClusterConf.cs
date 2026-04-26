namespace HamBusLog.Wa1gonLib.Models;

public record DxClusterConf(string Host)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Host { get; set; } = ValidateString(Host, nameof(Host));
    public int Port { get; set; } = 7300;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Guid LogConfigId { get; set; } // Explicit foreign key
    public LogConfig LogConfig { get; set; } = null!;
    public bool isDirty { get; set; } = false; // No [NotMapped]

    // {
    //     Console.WriteLine($"Creating DxClusterConf: Host={Host}");
    // }

    private static string ValidateString(string value, string propertyName)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }
}
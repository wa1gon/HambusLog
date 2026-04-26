namespace HamBusLog.Wa1gonLib.Models;

public record CallBookConf(string Name, string Host)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = ValidateString(Name, nameof(Name));
    public string Host { get; set; } = ValidateString(Host, nameof(Host));
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? ApiKey { get; set; }
    public Guid LogConfigId { get; set; } // Explicit foreign key
    public LogConfig LogConfig { get; set; } = null!;
    public bool isDirty { get; set; } = false; // No [NotMapped]

    private static string ValidateString(string value, string propertyName)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }
}

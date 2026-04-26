namespace HamBusLog.Wa1gonLib.Models;

public record class CallSign
{
    [Required] private string _call = string.Empty;

    [Key]
    public string Call
    {
        get => _call;
        set => _call = value?.ToUpperInvariant() ?? string.Empty;
    }

    public bool IsPrimary { get; set; } = true;
    public Guid OperatorId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.MinValue;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
    public string Class { get; set; } = string.Empty;
}

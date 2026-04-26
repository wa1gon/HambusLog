namespace HamBusLog.Wa1gonLib.Models;

public record OperatorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<CallSign> CallSigns { get; set; } = new();
}
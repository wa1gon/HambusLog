

namespace HamBusLog.Wa1gonLib.Models;

public class QsoQslInfo
{
    [Key] public int Id { get; set; }

    [MaxLength(20)] public required string QslService { get; set; }

    public bool QslSent { get; set; } = false;
    public bool QslReceived { get; set; } = false;

    // Foreign key to Qso (Guid)
    public Guid QsoId { get; set; }

    [ForeignKey(nameof(QsoId))]
    [JsonIgnore]
    public Qso Qso { get; set; } = null!;
}

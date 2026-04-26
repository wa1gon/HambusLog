using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HamBusLog.Wa1gonLib.Models;

public class QsoDetail
{
    [Key] public int Id { get; set; }

    public Guid QsoId { get; set; }

    [MaxLength(30)] [Required] public required string FieldName { get; set; } // e.g., "arrl_section", "class", "QslVia"

    [MaxLength(255)] [Required] public required string FieldValue { get; set; }

    [ForeignKey(nameof(QsoId))]
    [JsonIgnore]
    public Qso? Qso { get; set; } = null;
}
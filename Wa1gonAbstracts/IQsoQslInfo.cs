namespace HBAbstractions;

public interface IQsoQslInfo
{
    int Id { get; set; }
    string QslService { get; set; }
    bool QslSent { get; set; }
    bool QslReceived { get; set; }
    Guid QsoId { get; set; }
    IQso Qso { get; set; }
}
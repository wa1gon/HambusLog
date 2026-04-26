namespace HBAbstractions;

public interface IQso
{
    Guid Id { get; set; }
    string Call { get; set; }
    string MyCall { get; set; }
    DateTime QsoDate { get; set; }
    string Mode { get; set; }
    string ContestId { get; set; }
    string Country { get; set; }
    string State { get; set; }
    decimal Freq { get; set; }
    string Band { get; set; }
    string RstSent { get; set; }
    string RstRcvd { get; set; }
    int Dxcc { get; set; }
    bool BackedUp { get; set; }
    DateTime BackupDate { get; set; }
    DateTime LastUpdate { get; set; }
    ICollection<IQsoQslInfo> QslInfo { get; set; }
    ICollection<IQsoDetail> Details { get; set; }
    string ToString();
}
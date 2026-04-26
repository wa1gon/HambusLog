namespace HBAbstractions;

public interface IQsoDetail
{
    int Id { get; set; }
    Guid QsoId { get; set; }
    string FieldName { get; set; }
    string FieldValue { get; set; }
    IQso? Qso { get; set; }
}
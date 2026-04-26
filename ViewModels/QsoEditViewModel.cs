using HamBusLog.Wa1gonLib.Models;

namespace HamBusLog.ViewModels;

public sealed partial class QsoEditViewModel : ObservableObject
{
    [ObservableProperty] private string _call = string.Empty;
    [ObservableProperty] private string _band = string.Empty;
    [ObservableProperty] private string _mode = string.Empty;
    [ObservableProperty] private string _rstSent = string.Empty;
    [ObservableProperty] private string _rstRcvd = string.Empty;
    [ObservableProperty] private string _freq = string.Empty;
    [ObservableProperty] private string _qsoDateText = string.Empty;

    [ObservableProperty] private string _newDetailField = string.Empty;
    [ObservableProperty] private string _newDetailValue = string.Empty;
    [ObservableProperty] private QsoDetail? _selectedDetail;

    public ObservableCollection<QsoDetail> Details { get; } = [];

    public void LoadFrom(Qso qso)
    {
        Call = qso.Call ?? string.Empty;
        Band = qso.Band ?? string.Empty;
        Mode = qso.Mode ?? string.Empty;
        RstSent = qso.RstSent ?? string.Empty;
        RstRcvd = qso.RstRcvd ?? string.Empty;
        Freq = qso.Freq.ToString("0.###");
        var dt = qso.QsoDate == default ? DateTime.Now : qso.QsoDate;
        QsoDateText = dt.ToString("yyyy-MM-dd HH:mm");

        Details.Clear();
        if (qso.Details is { Count: > 0 })
        {
            foreach (var detail in qso.Details)
            {
                Details.Add(new QsoDetail
                {
                    FieldName = detail.FieldName,
                    FieldValue = detail.FieldValue,
                    QsoId = qso.Id
                });
            }
        }
    }

    public void AddDetail()
    {
        var field = (NewDetailField ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(field))
            return;

        Details.Add(new QsoDetail
        {
            FieldName = field,
            FieldValue = (NewDetailValue ?? string.Empty).Trim()
        });

        NewDetailField = string.Empty;
        NewDetailValue = string.Empty;
    }

    public void RemoveSelectedDetail()
    {
        if (SelectedDetail is null)
            return;

        Details.Remove(SelectedDetail);
        SelectedDetail = null;
    }

    public Qso BuildUpdatedQso(Guid id)
    {
        var freq = decimal.TryParse(Freq, out var parsedFreq) ? parsedFreq : 0m;
        var qsoDate = DateTime.TryParse(QsoDateText, out var parsedDate) ? parsedDate : DateTime.Now;
        var copy = new Qso
        {
            Id = id,
            Call = (Call ?? string.Empty).Trim().ToUpperInvariant(),
            Band = (Band ?? string.Empty).Trim().ToUpperInvariant(),
            Mode = (Mode ?? string.Empty).Trim().ToUpperInvariant(),
            RstSent = (RstSent ?? string.Empty).Trim(),
            RstRcvd = (RstRcvd ?? string.Empty).Trim(),
            Freq = freq,
            QsoDate = qsoDate,
            Details = [],
            QslInfo = []
        };

        foreach (var detail in Details)
        {
            copy.Details.Add(new QsoDetail
            {
                QsoId = id,
                FieldName = detail.FieldName,
                FieldValue = detail.FieldValue
            });
        }

        return copy;
    }
}



using HamBusLog.Models;
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
    [ObservableProperty] private string _stationCallSign = string.Empty;
    [ObservableProperty] private string _operator = string.Empty;
    [ObservableProperty] private string _state = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _section = string.Empty;
    [ObservableProperty] private string _qsoClass = string.Empty;

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
        StationCallSign = qso.StationCallSign ?? string.Empty;
        Operator = qso.Details?.FirstOrDefault(x =>
                string.Equals(x.FieldName, "OPERATOR", StringComparison.OrdinalIgnoreCase))?.FieldValue
            ?? StationCallSign;
        var dt = qso.QsoDate == default ? DateTime.Now : qso.QsoDate;
        QsoDateText = dt.ToString("yyyy-MM-dd HH:mm");
        State = qso.State ?? string.Empty;
        Country = qso.Country ?? string.Empty;
        Name = qso.Details?.FirstOrDefault(x => string.Equals(x.FieldName, "NAME", StringComparison.OrdinalIgnoreCase))?.FieldValue
            ?? string.Empty;
        Section = GetDetailValue(qso.Details, "SECTION", "ARRL_SECT", "ARRL-SECTION");
        QsoClass = GetDetailValue(qso.Details, "CLASS", "FD_CLASS");

        Details.Clear();
        if (qso.Details is { Count: > 0 })
        {
            foreach (var detail in qso.Details)
            {
                if (IsSectionOrClassField(detail.FieldName))
                    continue;

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
            StationCallSign = (StationCallSign ?? string.Empty).Trim().ToUpperInvariant(),
            State = (State ?? string.Empty).Trim().ToUpperInvariant(),
            Country = (Country ?? string.Empty).Trim().ToUpperInvariant(),
            Details = [],
            QslInfo = []
        };

        var operatorCall = string.IsNullOrWhiteSpace(Operator)
            ? copy.StationCallSign
            : Operator.Trim().ToUpperInvariant();
        copy.Details.Add(new QsoDetail
        {
            QsoId = id,
            FieldName = "OPERATOR",
            FieldValue = operatorCall
        });

        foreach (var detail in Details)
        {
            if (string.Equals(detail.FieldName, "OPERATOR", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(detail.FieldName, "NAME", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsSectionOrClassField(detail.FieldName))
                continue;

            copy.Details.Add(new QsoDetail
            {
                QsoId = id,
                FieldName = detail.FieldName,
                FieldValue = detail.FieldValue
            });
        }

        UpsertDetail(copy.Details, "NAME", Name);
        UpsertDetail(copy.Details, "SECTION", (Section ?? string.Empty).Trim().ToUpperInvariant());
        UpsertDetail(copy.Details, "CLASS", (QsoClass ?? string.Empty).Trim().ToUpperInvariant());

        return copy;
    }

    public void ApplyLookupResult(CallsignLookupResult result)
    {
        if (result is null)
            return;

        if (!string.IsNullOrWhiteSpace(result.CallSign))
            Call = result.CallSign;

        if (!string.IsNullOrWhiteSpace(result.State))
            State = result.State;

        if (!string.IsNullOrWhiteSpace(result.Country))
            Country = result.Country;

        if (!string.IsNullOrWhiteSpace(result.Name))
            Name = result.Name;

        UpsertDetailRow("COUNTY", result.County);
        UpsertDetailRow("GRID", result.Grid);
    }

    private void UpsertDetailRow(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(value))
            return;

        var existing = Details.FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Details.Add(new QsoDetail { FieldName = fieldName, FieldValue = value.Trim() });
            return;
        }

        existing.FieldValue = value.Trim();
    }

    private static void UpsertDetail(ICollection<QsoDetail> details, string fieldName, string? value)
    {
        if (details is null || string.IsNullOrWhiteSpace(fieldName))
            return;

        var normalizedValue = (value ?? string.Empty).Trim();
        var existing = details.FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
                return;

            details.Add(new QsoDetail
            {
                FieldName = fieldName,
                FieldValue = normalizedValue
            });
            return;
        }

        existing.FieldValue = normalizedValue;
    }

    private static bool IsSectionOrClassField(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return false;

        return string.Equals(fieldName, "SECTION", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fieldName, "ARRL_SECT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fieldName, "ARRL-SECTION", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fieldName, "CLASS", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fieldName, "FD_CLASS", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDetailValue(ICollection<QsoDetail>? details, params string[] fieldNames)
    {
        if (details is null || fieldNames is null || fieldNames.Length == 0)
            return string.Empty;

        foreach (var fieldName in fieldNames)
        {
            var value = details.FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.FieldValue;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
}

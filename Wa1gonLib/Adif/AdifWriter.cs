namespace HamBusLog.Wa1gonLib.Adif;

public static class AdifWriter
{
    public static string WriteToAdif(IEnumerable<Qso> qsos)
    {
        var sb = new StringBuilder();

        // Custom header comment
        sb.AppendLine("# ADIF with WA1GON GUID extensions");
        sb.AppendLine("<ADIF_VER:5>3.1.0");
        sb.AppendLine("<PROGRAMID:10>HamBlocks");
        sb.AppendLine($"<CREATED_TIMESTAMP:{DateTime.UtcNow:yyyyMMdd HHmmss}>");
        sb.AppendLine("<EOH>");

        foreach (var qso in qsos)
        {
            // Validation: must have at least BAND or FREQ
            var hasBand = !string.IsNullOrWhiteSpace(qso.Band);
            var hasFreq = qso.Freq != decimal.Zero;

            if (!hasBand && !hasFreq)
                throw new InvalidOperationException($"QSO with CALL {qso.Call} must have either BAND or FREQ.");

            // Core fields
            AppendField(sb, "CALL", qso.Call);

            if (hasBand)
                AppendField(sb, "BAND", qso.Band);

            if (hasFreq)
                AppendField(sb, "FREQ", qso.Freq.ToString("0.000", CultureInfo.InvariantCulture));

            AppendField(sb, "MODE", qso.Mode);
            AppendField(sb, "COUNTRY", qso.Country);
            AppendField(sb, "STATE", qso.State);
            if (qso.Dxcc > 0)
                AppendField(sb, "DXCC", qso.Dxcc.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(qso.RstSent))
                AppendField(sb, "RST_SENT", qso.RstSent);

            if (!string.IsNullOrWhiteSpace(qso.RstRcvd))
                AppendField(sb, "RST_RCVD", qso.RstRcvd);

            // GUID if available
            if (!string.IsNullOrWhiteSpace(qso.Id.ToString()))
                AppendField(sb, "GUID", qso.Id.ToString());

            // Extra fields from QsoDetail
            foreach (var detail in qso.Details)
                if (!string.IsNullOrWhiteSpace(detail.FieldName) && !string.IsNullOrWhiteSpace(detail.FieldValue))
                    AppendField(sb, detail.FieldName.ToUpperInvariant(), EscapeAdif(detail.FieldValue));

            sb.AppendLine("<EOR>");
        }

        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        sb.AppendFormat("<{0}:{1}>{2} ", name.ToUpperInvariant(), value.Length, value);
    }

    private static string EscapeAdif(string value)
    {
        return value.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
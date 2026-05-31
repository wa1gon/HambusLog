namespace HamBusLog.Services;

using System.Globalization;
using System.Xml.Linq;
using HamBusLog.Models;

public sealed class QrzLookupProvider : ICallsignLookupProvider
{
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly Uri BaseUri = new("https://xmldata.qrz.com/xml/current/");
    private static readonly bool QrzTraceEnabled = IsTraceEnabled();
    private static readonly string QrzTraceDirectory = GetTraceDirectory();
    private readonly QrzLookupConfiguration _config;
    private string? _sessionKey;

    public QrzLookupProvider(QrzLookupConfiguration config)
    {
        _config = config ?? new QrzLookupConfiguration();
    }

    public string ProviderName => "QRZ.com";

    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(_config.Username)
           && !string.IsNullOrWhiteSpace(_config.Password);

    public async Task<CallsignLookupResult?> LookupAsync(string callSign, CancellationToken cancellationToken)
    {
        var normalized = (callSign ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Callsign is required.");

        var sessionKey = await GetSessionKeyAsync(cancellationToken);
        var lookupUri = new Uri(BaseUri, $"?{BuildQuery(("s", sessionKey), ("callsign", normalized))}");
        var payload = await HttpClient.GetStringAsync(lookupUri, cancellationToken);
        TracePayload("lookup", payload);
        var doc = XDocument.Parse(payload);

        var sessionError = ReadSessionError(doc);
        if (!string.IsNullOrWhiteSpace(sessionError))
        {
            if (sessionError.Contains("Session", StringComparison.OrdinalIgnoreCase))
            {
                _sessionKey = null;
                sessionKey = await GetSessionKeyAsync(cancellationToken);
                lookupUri = new Uri(BaseUri, $"?{BuildQuery(("s", sessionKey), ("callsign", normalized))}");
                payload = await HttpClient.GetStringAsync(lookupUri, cancellationToken);
                TracePayload("lookup", payload);
                doc = XDocument.Parse(payload);
                sessionError = ReadSessionError(doc);
            }
        }

        if (!string.IsNullOrWhiteSpace(sessionError))
            throw new InvalidOperationException($"QRZ lookup failed: {sessionError}");

        var callElement = GetElement(doc.Root, "Callsign");
        if (callElement is null)
            return null;

        var firstName = ReadElementValue(callElement, "fname");
        var lastName = ReadElementValue(callElement, "name");
        var name = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var result = new CallsignLookupResult
        {
            Provider = ProviderName,
            CallSign = ReadElementValue(callElement, "call") ?? normalized,
            Name = name,
            Country = ReadElementValue(callElement, "country") ?? string.Empty,
            State = ReadElementValue(callElement, "state") ?? string.Empty,
            County = ReadElementValue(callElement, "county") ?? string.Empty,
            Grid = ReadElementValue(callElement, "grid") ?? string.Empty,
            Dxcc = ParseInt(ReadElementValue(callElement, "dxcc")),
            ItuZone = ParseInt(ReadElementValue(callElement, "itu")),
            CqZone = ParseInt(ReadElementValue(callElement, "cq"))
        };

        return result;
    }

    private async Task<string> GetSessionKeyAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_sessionKey))
            return _sessionKey;

        var user = _config.Username?.Trim() ?? string.Empty;
        var password = _config.Password?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("QRZ credentials are missing.");

        var loginUri = new Uri(BaseUri,
            $"?{BuildQuery(("username", user), ("password", password), ("agent", "HamBusLog"))}");
        var payload = await HttpClient.GetStringAsync(loginUri, cancellationToken);
        TracePayload("login", payload);
        var doc = XDocument.Parse(payload);

        var sessionError = ReadSessionError(doc);
        if (!string.IsNullOrWhiteSpace(sessionError))
            throw new InvalidOperationException($"QRZ login failed: {sessionError}");

        var session = GetElement(doc.Root, "Session");
        var key = session is null ? null : ReadElementValue(session, "Key");
        if (string.IsNullOrWhiteSpace(key))
        {
            var message = session is null ? null : ReadElementValue(session, "Message");
            var error = session is null ? null : ReadElementValue(session, "Error");
            var detail = !string.IsNullOrWhiteSpace(error)
                ? $" Error: {error}"
                : !string.IsNullOrWhiteSpace(message) && !string.Equals(message, "OK", StringComparison.OrdinalIgnoreCase)
                    ? $" Message: {message}"
                    : session is null
                        ? " Session element not found (check XML namespace)."
                        : $" Response root: {doc.Root?.Name.LocalName ?? "unknown"}";
            throw new InvalidOperationException(
                $"QRZ login failed: no session key returned.{detail} Check XML subscription and credentials.");
        }

        _sessionKey = key;
        return key;
    }

    private static string ReadSessionError(XDocument doc)
    {
        var session = GetElement(doc.Root, "Session");
        var error = session is null ? string.Empty : ReadElementValue(session, "Error") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(error))
            return error;

        var message = session is null ? string.Empty : ReadElementValue(session, "Message") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(message)
            && !string.Equals(message, "OK", StringComparison.OrdinalIgnoreCase))
            return message;

        return string.Empty;
    }

    private static string? ReadElementValue(XElement parent, string name)
    {
        var element = GetElement(parent, name);
        return element?.Value?.Trim();
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/xml");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
        return client;
    }

    private static string BuildQuery(params (string Key, string Value)[] parameters)
    {
        return string.Join("&", parameters.Select(item =>
            $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));
    }

    private static string GetUserAgent()
    {
        var version = typeof(QrzLookupProvider).Assembly.GetName().Version;
        return version is null ? "HamBusLog" : $"HamBusLog/{version}";
    }

    private static bool IsTraceEnabled()
    {
        var value = Environment.GetEnvironmentVariable("HAMBUSLOG_QRZ_TRACE");
        value = "1";
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTraceDirectory()
    {
        if (!QrzTraceEnabled)
            return string.Empty;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var directory = Path.Combine(home, "HamBusLog", "logs", "qrz");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TracePayload(string label, string payload)
    {
        if (!QrzTraceEnabled || string.IsNullOrWhiteSpace(QrzTraceDirectory))
            return;

        try
        {
            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{label}.xml";
            var path = Path.Combine(QrzTraceDirectory, fileName);
            File.WriteAllText(path, payload ?? string.Empty);
        }
        catch
        {
            // Intentionally ignore trace failures.
        }
    }

    private static XElement? GetElement(XElement? parent, string name)
    {
        if (parent is null)
            return null;

        var ns = parent.Name.Namespace;
        return parent.Element(ns + name) ?? parent.Element(name);
    }
}


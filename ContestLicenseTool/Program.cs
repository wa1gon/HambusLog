using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var exitCode = Run(args);
return exitCode;

static int Run(string[] args)
{
    if (args.Length == 0 || IsHelp(args[0]))
    {
        PrintUsage();
        return 0;
    }

    var command = args[0].Trim().ToLowerInvariant();
    var options = ParseOptions(args.Skip(1).ToArray());

    try
    {
        return command switch
        {
            "canonical" => RunCanonical(options),
            "sha256" => RunSha256(options),
            "sign" => RunSign(options),
            "keygen" => RunKeygen(options),
            _ => Fail($"Unknown command '{command}'.")
        };
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static int RunCanonical(IReadOnlyDictionary<string, string> options)
{
    var fields = ResolveRequiredFields(options);
    Console.WriteLine(BuildCanonicalPayload(fields));
    return 0;
}

static int RunSha256(IReadOnlyDictionary<string, string> options)
{
    var fields = ResolveRequiredFields(options);
    var canonical = BuildCanonicalPayload(fields);
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    Console.WriteLine($"sha256:{Convert.ToHexString(hash)}");
    return 0;
}

static int RunSign(IReadOnlyDictionary<string, string> options)
{
    var fields = ResolveRequiredFields(options);
    var canonical = BuildCanonicalPayload(fields);
    var payloadBytes = Encoding.UTF8.GetBytes(canonical);

    var algorithm = GetOption(options, "alg")?.ToLowerInvariant() ?? "rsa-sha256";
    var privateKeyBytes = ResolvePrivateKey(options);

    string publicKey;
    string signature;

    switch (algorithm)
    {
        case "rsa-sha256":
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            signature = Convert.ToBase64String(rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            break;
        }
        case "ecdsa-sha256":
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
            signature = Convert.ToBase64String(ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256));
            break;
        }
        default:
            return Fail("Unsupported --alg value. Use rsa-sha256 or ecdsa-sha256.");
    }

    Console.WriteLine($"sig:{algorithm}:{publicKey}:{signature}");
    return 0;
}

static int RunKeygen(IReadOnlyDictionary<string, string> options)
{
    var algorithm = GetOption(options, "alg")?.ToLowerInvariant() ?? "rsa-sha256";

    switch (algorithm)
    {
        case "rsa-sha256":
        {
            using var rsa = RSA.Create(2048);
            Console.WriteLine("Algorithm: rsa-sha256");
            Console.WriteLine($"PrivateKeyPkcs8Base64: {Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())}");
            Console.WriteLine($"PublicKeySpkiBase64: {Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo())}");
            return 0;
        }
        case "ecdsa-sha256":
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            Console.WriteLine("Algorithm: ecdsa-sha256");
            Console.WriteLine($"PrivateKeyPkcs8Base64: {Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey())}");
            Console.WriteLine($"PublicKeySpkiBase64: {Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo())}");
            return 0;
        }
        default:
            return Fail("Unsupported --alg value. Use rsa-sha256 or ecdsa-sha256.");
    }
}

static Dictionary<string, string> ResolveRequiredFields(IReadOnlyDictionary<string, string> options)
{
    var fieldsOption = GetOption(options, "fields");
    if (!string.IsNullOrWhiteSpace(fieldsOption))
        return ParseFieldsOption(fieldsOption!);

    var contestFile = GetOption(options, "contest-file");
    if (string.IsNullOrWhiteSpace(contestFile))
        throw new InvalidOperationException("Provide --fields or --contest-file.");

    var contestKey = GetOption(options, "contest-key");
    return ParseFieldsFromContestJson(contestFile!, contestKey);
}

static Dictionary<string, string> ParseFieldsOption(string fieldsOption)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var entries = fieldsOption.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var entry in entries)
    {
        var separatorIndex = entry.IndexOf('=');
        if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
            throw new InvalidOperationException($"Invalid --fields entry '{entry}'. Use key=value;key2=value2.");

        var key = entry[..separatorIndex].Trim();
        var value = entry[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Field keys and values must be non-empty.");

        result[key] = value;
    }

    if (result.Count == 0)
        throw new InvalidOperationException("No valid fields were provided.");

    return result;
}

static Dictionary<string, string> ParseFieldsFromContestJson(string path, string? contestKey)
{
    if (!File.Exists(path))
        throw new InvalidOperationException($"Contest file not found: {path}");

    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var contestElements = ResolveContestElements(document.RootElement);
    if (contestElements is null || contestElements.Count == 0)
        throw new InvalidOperationException("Contest JSON must be an array, a single contest object, or contain a 'contests' array.");

    JsonElement contestElement;
    if (string.IsNullOrWhiteSpace(contestKey))
    {
        contestElement = contestElements[0];
    }
    else
    {
        contestElement = contestElements
            .FirstOrDefault(x => string.Equals(ResolveContestKey(x), contestKey.Trim(), StringComparison.OrdinalIgnoreCase));

        if (contestElement.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"Contest key '{contestKey}' was not found in {path}.");
    }

    var requiredFields = ParseRequiredFields(contestElement);
    if (requiredFields.Count == 0)
        throw new InvalidOperationException("Selected contest has no requiredFields to license.");

    return requiredFields;
}

static List<JsonElement>? ResolveContestElements(JsonElement root)
{
    if (root.ValueKind == JsonValueKind.Array)
        return root.EnumerateArray().ToList();

    if (root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("contests", out var contests)
        && contests.ValueKind == JsonValueKind.Array)
    {
        return contests.EnumerateArray().ToList();
    }

    if (root.ValueKind == JsonValueKind.Object)
        return [root];

    return null;
}

static string? ResolveContestKey(JsonElement contestElement)
{
    var directKey = ReadString(contestElement, "key");
    if (!string.IsNullOrWhiteSpace(directKey))
        return directKey;

    if (contestElement.TryGetProperty("group", out var groupElement))
    {
        if (groupElement.ValueKind == JsonValueKind.String)
            return groupElement.GetString()?.Trim();

        if (groupElement.ValueKind == JsonValueKind.Object)
            return ReadString(groupElement, "key") ?? ReadString(groupElement, "name");
    }

    return ReadString(contestElement, "adifContestId");
}

static Dictionary<string, string> ParseRequiredFields(JsonElement contestElement)
{
    var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!contestElement.TryGetProperty("requiredFields", out var requiredFieldsElement))
        return results;

    if (requiredFieldsElement.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in requiredFieldsElement.EnumerateObject())
        {
            var key = property.Name.Trim();
            var value = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()?.Trim() ?? string.Empty
                : property.Value.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                results[key] = value;
        }

        return results;
    }

    if (requiredFieldsElement.ValueKind != JsonValueKind.Array)
        return results;

    foreach (var field in requiredFieldsElement.EnumerateArray())
    {
        if (field.ValueKind != JsonValueKind.Object)
            continue;

        var key = ReadString(field, "key");
        var detailFieldName = ReadString(field, "detailFieldName");
        var label = ReadString(field, "label");
        var value = string.IsNullOrWhiteSpace(detailFieldName)
            ? (string.IsNullOrWhiteSpace(label) ? key : label)
            : detailFieldName;

        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            results[key] = value;
    }

    return results;
}

static string BuildCanonicalPayload(IReadOnlyDictionary<string, string> values)
{
    var ordered = values
        .Select(x => new KeyValuePair<string, string>(x.Key.Trim().ToLowerInvariant(), x.Value.Trim()))
        .OrderBy(x => x.Key, StringComparer.Ordinal)
        .ThenBy(x => x.Value, StringComparer.Ordinal)
        .ToList();

    return string.Join("\n", ordered.Select(x => $"{x.Key}={x.Value}"));
}

static byte[] ResolvePrivateKey(IReadOnlyDictionary<string, string> options)
{
    var keyFile = GetOption(options, "private-key-file");
    if (!string.IsNullOrWhiteSpace(keyFile))
        return DecodeBase64(File.ReadAllText(keyFile!).Trim(), "private key file");

    var keyBase64 = GetOption(options, "private-key-base64");
    if (!string.IsNullOrWhiteSpace(keyBase64))
        return DecodeBase64(keyBase64!, "--private-key-base64");

    throw new InvalidOperationException("sign requires --private-key-file or --private-key-base64.");
}

static byte[] DecodeBase64(string value, string sourceName)
{
    try
    {
        return Convert.FromBase64String(value);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"{sourceName} is not valid Base64: {ex.Message}");
    }
}

static string? ReadString(JsonElement element, string propertyName)
{
    if (!element.TryGetProperty(propertyName, out var property))
        return null;

    return property.ValueKind == JsonValueKind.String
        ? property.GetString()?.Trim()
        : property.ToString().Trim();
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected argument '{arg}'. Options must start with --.");

        var key = arg[2..];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Option name cannot be empty.");

        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            throw new InvalidOperationException($"Option '--{key}' requires a value.");

        result[key] = args[i + 1];
        i++;
    }

    return result;
}

static string? GetOption(IReadOnlyDictionary<string, string> options, string key)
    => options.TryGetValue(key, out var value) ? value : null;

static bool IsHelp(string arg)
    => arg is "-h" or "--help" or "help";

static void PrintUsage()
{
    Console.WriteLine("ContestLicenseTool - build deterministic contest license keys");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  canonical   Print canonical payload");
    Console.WriteLine("  sha256      Print sha256 license key");
    Console.WriteLine("  sign        Print signature license key");
    Console.WriteLine("  keygen      Generate keypair");
    Console.WriteLine();
    Console.WriteLine("Input options (canonical/sha256/sign):");
    Console.WriteLine("  --fields \"name=Name;state=State\"");
    Console.WriteLine("  --contest-file /path/to/contest.json [--contest-key SPRINT]");
    Console.WriteLine();
    Console.WriteLine("Sign options:");
    Console.WriteLine("  --alg rsa-sha256|ecdsa-sha256");
    Console.WriteLine("  --private-key-file /path/to/private-key.base64");
    Console.WriteLine("  --private-key-base64 <PKCS8 base64>");
    Console.WriteLine();
    Console.WriteLine("Keygen options:");
    Console.WriteLine("  --alg rsa-sha256|ecdsa-sha256");
}

static int Fail(string message)
{
    Console.Error.WriteLine("Error: " + message);
    Console.Error.WriteLine("Use --help for usage.");
    return 1;
}


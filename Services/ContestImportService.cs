using System.Security.Cryptography;
using System.Text;

namespace HamBusLog.Services;

public interface IContestLicenseValidationService
{
    ContestLicenseValidationResult ValidateLicense(string licenseKey, IReadOnlyDictionary<string, string> requiredFieldNameValues);
}

public sealed record ContestLicenseValidationResult(bool IsValid, string ErrorMessage)
{
    public static ContestLicenseValidationResult Success() => new(true, string.Empty);
    public static ContestLicenseValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

public sealed class ContestLicenseValidationService : IContestLicenseValidationService
{
    public ContestLicenseValidationResult ValidateLicense(string licenseKey, IReadOnlyDictionary<string, string> requiredFieldNameValues)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return ContestLicenseValidationResult.Failure("License key is required.");

        if (requiredFieldNameValues.Count == 0)
            return ContestLicenseValidationResult.Failure("At least one required field is needed for license validation.");

        foreach (var pair in requiredFieldNameValues)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                return ContestLicenseValidationResult.Failure("Required field names cannot be empty.");
            if (string.IsNullOrWhiteSpace(pair.Value))
                return ContestLicenseValidationResult.Failure($"Required field '{pair.Key}' must provide a name/value mapping.");
        }

        var canonicalPayload = BuildCanonicalPayload(requiredFieldNameValues);
        var trimmedLicense = licenseKey.Trim();

        if (trimmedLicense.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            return ValidateSha256License(trimmedLicense, canonicalPayload);

        if (trimmedLicense.StartsWith("sig:", StringComparison.OrdinalIgnoreCase))
            return ValidateSignatureLicense(trimmedLicense, canonicalPayload);

        return ContestLicenseValidationResult.Failure("Unsupported license format. Use 'sha256:<hex>' or 'sig:<rsa-sha256|ecdsa-sha256>:<base64PublicKey>:<base64Signature>'.");
    }

    private static ContestLicenseValidationResult ValidateSha256License(string licenseKey, string canonicalPayload)
    {
        var expectedHex = licenseKey["sha256:".Length..].Trim();
        if (string.IsNullOrWhiteSpace(expectedHex))
            return ContestLicenseValidationResult.Failure("SHA256 license format is invalid. Expected 'sha256:<hex>'.");

        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        var expectedHash = TryDecodeHex(expectedHex);
        if (expectedHash is null)
            return ContestLicenseValidationResult.Failure("SHA256 license hash is not valid hexadecimal.");

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)
            ? ContestLicenseValidationResult.Success()
            : ContestLicenseValidationResult.Failure("SHA256 license hash did not match required fields.");
    }

    private static ContestLicenseValidationResult ValidateSignatureLicense(string licenseKey, string canonicalPayload)
    {
        var parts = licenseKey.Split(':', 4, StringSplitOptions.None);
        if (parts.Length != 4)
            return ContestLicenseValidationResult.Failure("Signature license format is invalid. Expected 'sig:<rsa-sha256|ecdsa-sha256>:<base64PublicKey>:<base64Signature>'.");

        var algorithm = parts[1].Trim().ToLowerInvariant();
        if (algorithm is not ("rsa-sha256" or "ecdsa-sha256"))
            return ContestLicenseValidationResult.Failure("Unsupported signature algorithm. Use 'rsa-sha256' or 'ecdsa-sha256'.");

        if (!TryDecodeBase64(parts[2].Trim(), out var publicKeyBytes))
            return ContestLicenseValidationResult.Failure("Signature public key is not valid Base64.");

        if (!TryDecodeBase64(parts[3].Trim(), out var signatureBytes))
            return ContestLicenseValidationResult.Failure("Signature value is not valid Base64.");

        var payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);

        try
        {
            var isValid = algorithm == "rsa-sha256"
                ? VerifyRsaSha256(publicKeyBytes, payloadBytes, signatureBytes)
                : VerifyEcdsaSha256(publicKeyBytes, payloadBytes, signatureBytes);

            return isValid
                ? ContestLicenseValidationResult.Success()
                : ContestLicenseValidationResult.Failure("Digital signature did not match required fields.");
        }
        catch (Exception ex)
        {
            return ContestLicenseValidationResult.Failure("Signature validation failed: " + ex.Message);
        }
    }

    private static bool VerifyRsaSha256(byte[] publicKeyBytes, byte[] payloadBytes, byte[] signatureBytes)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyEcdsaSha256(byte[] publicKeyBytes, byte[] payloadBytes, byte[] signatureBytes)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256);
    }

    private static string BuildCanonicalPayload(IReadOnlyDictionary<string, string> requiredFieldNameValues)
    {
        var orderedPairs = requiredFieldNameValues
            .Select(pair => new KeyValuePair<string, string>(
                pair.Key.Trim().ToLowerInvariant(),
                pair.Value.Trim()))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ThenBy(pair => pair.Value, StringComparer.Ordinal)
            .ToList();

        return string.Join("\n", orderedPairs.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static byte[]? TryDecodeHex(string hex)
    {
        var normalized = hex.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..];

        if ((normalized.Length & 1) == 1)
            return null;

        try
        {
            return Convert.FromHexString(normalized);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryDecodeBase64(string input, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(input);
            return true;
        }
        catch
        {
            bytes = [];
            return false;
        }
    }
}

public interface IContestImportService
{
    ContestImportResult ImportContests(string rawJson, string? licenseKeyOverride = null);
}

public sealed record ContestImportResult(bool Success, IReadOnlyList<ContestDefinitionConfig> Contests, string ErrorMessage)
{
    public static ContestImportResult Fail(string errorMessage) => new(false, [], errorMessage);
    public static ContestImportResult Ok(IReadOnlyList<ContestDefinitionConfig> contests) => new(true, contests, string.Empty);
}

public sealed class ContestImportService : IContestImportService
{
    private readonly IContestLicenseValidationService _licenseValidationService;

    public ContestImportService(IContestLicenseValidationService? licenseValidationService = null)
    {
        _licenseValidationService = licenseValidationService ?? new ContestLicenseValidationService();
    }

    public ContestImportResult ImportContests(string rawJson, string? licenseKeyOverride = null)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return ContestImportResult.Fail("Contest import payload is empty.");

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var contestElements = ResolveContestElements(document.RootElement);
            if (contestElements is null)
                return ContestImportResult.Fail("Contest import JSON must be an array, a single contest object, or contain a 'contests' array.");

            var normalized = new List<ContestDefinitionConfig>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trimmedOverride = licenseKeyOverride?.Trim();

            foreach (var contestElement in contestElements)
            {
                var adifContestId = ReadString(contestElement, "adifContestId");
                var key = ResolveContestKey(contestElement, adifContestId);
                if (string.IsNullOrWhiteSpace(key))
                    return ContestImportResult.Fail("Each imported contest must define 'key', 'group', or 'adifContestId'.");

                if (!seenKeys.Add(key!))
                    return ContestImportResult.Fail($"Duplicate contest key '{key}' in import file.");

                var displayName = ReadString(contestElement, "displayName");
                var exchangeType = ReadString(contestElement, "exchangeType");
                var contestLicenseKey = string.IsNullOrWhiteSpace(trimmedOverride)
                    ? ReadString(contestElement, "licenseKey")
                    : trimmedOverride;

                var requiredFields = ParseRequiredFields(contestElement);
                var nameValueFields = requiredFields.ToDictionary(
                    x => x.Key,
                    x => x.DetailFieldName,
                    StringComparer.OrdinalIgnoreCase);

                var validation = _licenseValidationService.ValidateLicense(contestLicenseKey ?? string.Empty, nameValueFields);
                if (!validation.IsValid)
                    return ContestImportResult.Fail($"Contest '{key}': {validation.ErrorMessage}");

                normalized.Add(new ContestDefinitionConfig
                {
                    Key = key!,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? key! : displayName.Trim(),
                    AdifContestId = string.IsNullOrWhiteSpace(adifContestId) ? key! : adifContestId.Trim(),
                    ExchangeType = string.IsNullOrWhiteSpace(exchangeType) ? "normal" : exchangeType.Trim().ToLowerInvariant(),
                    LicenseKey = contestLicenseKey!.Trim(),
                    RequiredFields = requiredFields
                });
            }

            if (normalized.Count == 0)
                return ContestImportResult.Fail("Contest import file did not contain any contest definitions.");

            return ContestImportResult.Ok(normalized);
        }
        catch (Exception ex)
        {
            return ContestImportResult.Fail("Contest import JSON is invalid: " + ex.Message);
        }
    }

    private static List<JsonElement>? ResolveContestElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray().ToList();

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("contests", out var contests)
            && contests.ValueKind == JsonValueKind.Array)
        {
            return contests.EnumerateArray().ToList();
        }

        if (root.ValueKind == JsonValueKind.Object && LooksLikeContestObject(root))
        {
            return [root];
        }

        return null;
    }

    private static bool LooksLikeContestObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        return element.TryGetProperty("key", out _)
            || element.TryGetProperty("group", out _)
            || element.TryGetProperty("adifContestId", out _)
            || element.TryGetProperty("requiredFields", out _);
    }

    private static string? ResolveContestKey(JsonElement contestElement, string? adifContestId)
    {
        var directKey = ReadString(contestElement, "key");
        if (!string.IsNullOrWhiteSpace(directKey))
            return directKey;

        if (contestElement.TryGetProperty("group", out var groupElement))
        {
            if (groupElement.ValueKind == JsonValueKind.String)
            {
                var groupValue = groupElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(groupValue))
                    return groupValue;
            }
            else if (groupElement.ValueKind == JsonValueKind.Object)
            {
                var groupKey = ReadString(groupElement, "key");
                if (!string.IsNullOrWhiteSpace(groupKey))
                    return groupKey;

                var groupName = ReadString(groupElement, "name");
                if (!string.IsNullOrWhiteSpace(groupName))
                    return groupName;
            }
        }

        return adifContestId;
    }

    private static List<ContestFieldRequirementConfig> ParseRequiredFields(JsonElement contestElement)
    {
        var results = new List<ContestFieldRequirementConfig>();
        if (!contestElement.TryGetProperty("requiredFields", out var requiredFieldsElement))
            return results;

        if (requiredFieldsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in requiredFieldsElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                    continue;

                var value = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()?.Trim() ?? string.Empty
                    : property.Value.ToString();

                results.Add(new ContestFieldRequirementConfig
                {
                    Key = property.Name.Trim(),
                    Label = string.IsNullOrWhiteSpace(value) ? property.Name.Trim() : value,
                    DetailFieldName = value
                });
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
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var label = ReadString(field, "label");
            var detailFieldName = ReadString(field, "detailFieldName");

            results.Add(new ContestFieldRequirementConfig
            {
                Key = key.Trim(),
                Label = string.IsNullOrWhiteSpace(label) ? key.Trim() : label.Trim(),
                DetailFieldName = string.IsNullOrWhiteSpace(detailFieldName)
                    ? (string.IsNullOrWhiteSpace(label) ? key.Trim() : label.Trim())
                    : detailFieldName.Trim()
            });
        }

        return results;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : property.ToString().Trim();
    }
}




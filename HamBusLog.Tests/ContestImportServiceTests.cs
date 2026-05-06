using HamBusLog.Services;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace HamBusLog.Tests;

public sealed class ContestImportServiceTests
{
    [Fact]
    public void ImportContests_WithSingleContestObject_ImportsDefinition()
    {
        var service = new ContestImportService();
        var license = BuildSha256License(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "Name"
        });

        var payload =
            """
            {
              "group": {
                "key": "SST"
              },
              "displayName": "Slow Speed Test",
              "adifContestId": "SST",
              "requiredFields": {
                "name": "Name"
              }
            }
            """;

        var result = service.ImportContests(payload, license);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("SST", Assert.Single(result.Contests).Key);
    }

    [Fact]
    public void ImportContests_WithGroupString_UsesGroupAsKey()
    {
        var service = new ContestImportService();
        var license = BuildSha256License(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["class"] = "Class"
        });

        var payload =
            """
            [
              {
                "group": "WFD",
                "displayName": "Winter Field Day",
                "adifContestId": "WFD",
                "requiredFields": {
                  "class": "Class"
                }
              }
            ]
            """;

        var result = service.ImportContests(payload, license);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("WFD", Assert.Single(result.Contests).Key);
    }

    [Fact]
    public void ImportContests_WithShaLicenseOverride_ImportsDefinitions()
    {
        var service = new ContestImportService();
        var license = BuildSha256License(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "Name",
            ["state"] = "State"
        });

        var payload =
            """
            {
              "contests": [
                {
                  "key": "SPRINT",
                  "displayName": "Weekly Sprint",
                  "adifContestId": "SPRINT",
                  "exchangeType": "normal",
                  "requiredFields": {
                    "name": "Name",
                    "state": "State"
                  }
                }
              ]
            }
            """;

        var result = service.ImportContests(payload, license);

        Assert.True(result.Success, result.ErrorMessage);
        var contest = Assert.Single(result.Contests);
        Assert.Equal("SPRINT", contest.Key);
        Assert.Equal(license, contest.LicenseKey);
        Assert.Equal(2, contest.RequiredFields.Count);
    }

    [Fact]
    public void ImportContests_WithSha256License_IgnoresRequiredFieldOrder()
    {
        var service = new ContestImportService();
        var license = BuildSha256License(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "Name",
            ["state"] = "State"
        });

        var payloadA =
            """
            [
              {
                "key": "ORDER-A",
                "adifContestId": "ORDER-A",
                "requiredFields": {
                  "name": "Name",
                  "state": "State"
                }
              }
            ]
            """;

        var payloadB =
            """
            [
              {
                "key": "ORDER-B",
                "adifContestId": "ORDER-B",
                "requiredFields": {
                  "state": "State",
                  "name": "Name"
                }
              }
            ]
            """;

        var resultA = service.ImportContests(payloadA, license);
        var resultB = service.ImportContests(payloadB, license);

        Assert.True(resultA.Success, resultA.ErrorMessage);
        Assert.True(resultB.Success, resultB.ErrorMessage);
    }

    [Fact]
    public void ImportContests_WithRsaSignature_ValidatesSignature()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        var canonicalPayload = BuildCanonicalPayload(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "Name",
            ["state"] = "State"
        });
        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(canonicalPayload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var license = $"sig:rsa-sha256:{publicKey}:{signature}";

        var service = new ContestImportService();
        var payload =
            """
            [
              {
                "key": "SIGTEST",
                "adifContestId": "SIGTEST",
                "requiredFields": {
                  "state": "State",
                  "name": "Name"
                }
              }
            ]
            """;

        var result = service.ImportContests(payload, license);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public void ImportContests_WithFullLicenseOverride_ImportsAllContests()
    {
        var service = new ContestImportService();
        var payload =
            """
            {
              "contests": [
                {
                  "key": "ARQP-IN",
                  "adifContestId": "AR-QSO-PARTY",
                  "requiredFields": {
                    "state": "State/Province"
                  }
                },
                {
                  "key": "ARQP-OUT",
                  "adifContestId": "AR-QSO-PARTY",
                  "requiredFields": {
                    "county": "Arkansas County"
                  }
                }
              ]
            }
            """;

        var result = service.ImportContests(payload, "full:ALL-CONTESTS-2026");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(2, result.Contests.Count);
    }

    [Fact]
    public void ImportContests_WithMalformedFullLicense_FailsValidation()
    {
        var service = new ContestImportService();
        var payload =
            """
            [
              {
                "key": "BROKEN",
                "adifContestId": "BROKEN",
                "requiredFields": {
                  "name": "Name"
                }
              }
            ]
            """;

        var result = service.ImportContests(payload, "full:");

        Assert.False(result.Success);
        Assert.Contains("full license format is invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportContests_WithoutLicense_FailsValidation()
    {
        var service = new ContestImportService();
        var payload =
            """
            [
              {
                "key": "CQWW",
                "adifContestId": "CQ-WW",
                "requiredFields": {
                  "zone": "Zone"
                }
              }
            ]
            """;

        var result = service.ImportContests(payload);

        Assert.False(result.Success);
        Assert.Contains("License key is required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportContests_WithoutRequiredFieldValues_FailsValidation()
    {
        var service = new ContestImportService();
        var payload =
            """
            [
              {
                "key": "TEST",
                "adifContestId": "TEST",
                "licenseKey": "sha256:00",
                "requiredFields": {
                  "name": ""
                }
              }
            ]
            """;

        var result = service.ImportContests(payload);

        Assert.False(result.Success);
        Assert.Contains("name/value", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportContests_WithoutKeyGroupAndAdifContestId_FailsValidation()
    {
        var service = new ContestImportService();
        var payload =
            """
            [
              {
                "displayName": "Broken Contest",
                "licenseKey": "sha256:00",
                "requiredFields": {
                  "section": "Section"
                }
              }
            ]
            """;

        var result = service.ImportContests(payload);

        Assert.False(result.Success);
        Assert.Contains("group", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSha256License(IReadOnlyDictionary<string, string> values)
    {
        var canonical = BuildCanonicalPayload(values);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"sha256:{Convert.ToHexString(hash)}";
    }

    private static string BuildCanonicalPayload(IReadOnlyDictionary<string, string> values)
    {
        var ordered = values
            .Select(x => new KeyValuePair<string, string>(x.Key.Trim().ToLowerInvariant(), x.Value.Trim()))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .ToList();

        return string.Join("\n", ordered.Select(x => $"{x.Key}={x.Value}"));
    }
}






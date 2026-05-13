# ContestLicenseTool

Small console app to generate deterministic contest license keys for `HamBusLog`.

## Build

```bash
dotnet build /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj
```

## Commands

### 1) Show canonical payload

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- canonical --fields "name=Name;state=State"
```

### 2) Generate SHA-256 license key

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- sha256 --fields "name=Name;state=State"
```

Output format:

```text
sha256:<HEX>
```

### 3) Generate signing keypair

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- keygen --alg rsa-sha256
```

This prints:

- `PrivateKeyPkcs8Base64`
- `PublicKeySpkiBase64`

### 4) Generate digital signature license key

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- sign --alg rsa-sha256 --private-key-base64 "<PKCS8_BASE64>" --fields "name=Name;state=State"
```

Output format:

```text
sig:rsa-sha256:<base64PublicKey>:<base64Signature>
```

### 5) Generate full-access license key

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- full
```

Output format:

```text
full:<TOKEN>
```

You can provide your own token:

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- full --token "ALL-CONTESTS-2026"
```

## JSON input mode

Instead of `--fields`, you can read required fields directly from a contest JSON payload:

```bash
dotnet run --project /home/darryl/github/Hambus/HamBusLog/ContestLicenseTool/ContestLicenseTool.csproj -- sha256 --contest-file /path/to/contest.json --contest-key SPRINT
```

If `--contest-key` is omitted, the first contest item is used.


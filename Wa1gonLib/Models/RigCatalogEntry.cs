namespace Wa1gonLib.Models;

public sealed class RigCatalogEntry
{
    public required int RigNum { get; init; }

    public required string Mfg { get; init; }

    public required string Model { get; init; }

    public required string Version { get; init; }

    public required string Status { get; init; }

    public required string Macro { get; init; }
}


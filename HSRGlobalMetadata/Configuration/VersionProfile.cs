namespace HSRGlobalMetadata.Configuration;

public sealed record VersionProfile(
    string Name,
    uint MetadataMagic,
    ulong ImageBase,
    MetadataLayout Layout
) {
    public string MetadataMagicText => $"0x{MetadataMagic:X8}";
    public string ImageBaseText => $"0x{ImageBase:X}";
}

public static class VersionProfiles {
    public const string DefaultName = "OSPRODWin4.5.0";

    private static readonly Dictionary<string, VersionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase) {
        [DefaultName] = new VersionProfile(
            DefaultName,
            MetadataMagic: 0x0059484D,
            ImageBase: 0x180000000,
            MetadataLayout.OspProdWin450
        )
    };

    public static IReadOnlyCollection<string> Names => Profiles.Keys;

    public static bool TryGet(string name, out VersionProfile profile) => Profiles.TryGetValue(name, out profile!);
}

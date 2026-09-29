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
    public const string OspProdWin450Name = "OSPRODWin4.5.0";
    public const string OspProdWin460Name = "OSPRODWin4.6.0";
    public const string DefaultName = OspProdWin460Name;

    private static readonly Dictionary<string, VersionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase) {
        [OspProdWin450Name] = Create(OspProdWin450Name, MetadataLayout.OspProdWin450),
        [OspProdWin460Name] = Create(OspProdWin460Name, MetadataLayout.OspProdWin460)
    };

    public static IReadOnlyCollection<string> Names => Profiles.Keys;

    public static bool TryGet(string name, out VersionProfile profile) => Profiles.TryGetValue(name, out profile!);

    private static VersionProfile Create(string name, MetadataLayout layout) => new(
        name,
        MetadataMagic: 0x0059484D,
        ImageBase: 0x180000000,
        layout
    );
}

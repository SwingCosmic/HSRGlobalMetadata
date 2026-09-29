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
    public const string OsProdWin430Name = "OSPRODWin4.3.0";
    public const string OsProdWin440Name = "OSPRODWin4.4.0";
    public const string OsProdWin450Name = "OSPRODWin4.5.0";
    public const string OsProdWin460Name = "OSPRODWin4.6.0";
    public const string DefaultName = OsProdWin460Name;

    public const uint DefaultMetadataMagic = 0x0059484D;
    public const ulong DefaultImageBase = 0x180000000;

    // Profiles are recipes over two independently versioned modes:
    // Il2CppType records (IndexBased8 vs VaBased16) and GameAssembly registration slots
    // (OsProdWin440 vs OsProdWin450). 4.3→4.4 switched the type record; 4.4→4.5 shuffled
    // registration slots; 4.5→4.6 switched the type record back. A later version that
    // reuses both modes only needs a new dictionary entry here.
    private static readonly Dictionary<string, VersionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase) {
        [OsProdWin430Name] = Create(OsProdWin430Name, Il2CppTypeRecordLayout.IndexBased8, RegistrationLayout.OsProdWin440),
        [OsProdWin440Name] = Create(OsProdWin440Name, Il2CppTypeRecordLayout.VaBased16, RegistrationLayout.OsProdWin440),
        [OsProdWin450Name] = Create(OsProdWin450Name, Il2CppTypeRecordLayout.VaBased16, RegistrationLayout.OsProdWin450),
        [OsProdWin460Name] = Create(OsProdWin460Name, Il2CppTypeRecordLayout.IndexBased8, RegistrationLayout.OsProdWin450)
    };

    public static IReadOnlyCollection<string> Names => Profiles.Keys;

    public static bool TryGet(string name, out VersionProfile profile) => Profiles.TryGetValue(name, out profile!);

    public static VersionProfile Create(
        string name,
        Il2CppTypeRecordLayout typeRecord,
        RegistrationLayout registration
    ) => new(
        name,
        MetadataMagic: DefaultMetadataMagic,
        ImageBase: DefaultImageBase,
        MetadataLayout.Compose(typeRecord, registration)
    );
}

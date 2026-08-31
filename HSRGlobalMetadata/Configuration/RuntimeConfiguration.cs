namespace HSRGlobalMetadata.Configuration;

public static class RuntimeConfiguration {
    private static VersionProfile _current = GetDefault();

    public static VersionProfile Current => _current;

    public static void Initialize(VersionProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);
        _current = profile;
    }

    public static VersionProfile GetDefault() {
        if (!VersionProfiles.TryGet(VersionProfiles.DefaultName, out var profile))
            throw new InvalidOperationException($"Default version profile '{VersionProfiles.DefaultName}' is not registered.");
        return profile;
    }

    private static void Validate(VersionProfile profile) {
        if (profile.MetadataMagic == 0)
            throw new ArgumentOutOfRangeException(nameof(profile), "Metadata magic must not be zero.");
        if (profile.ImageBase == 0 || profile.ImageBase > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(profile), "ImageBase must be between 1 and Int64.MaxValue.");

        var layout = profile.Layout;
        var sizes = new[] {
            layout.MetadataHeaderSize,
            layout.TypeDefinitionSize,
            layout.ImageDefinitionSize,
            layout.MethodDefinitionSize,
            layout.FieldDefinitionSize,
            layout.PropertyDefinitionSize,
            layout.EventDefinitionSize,
            layout.ParameterDefinitionSize,
            layout.GenericContainerDefinitionSize,
            layout.GenericParameterDefinitionSize,
            layout.GenericFunctionDefinitionSize,
            layout.GenericParameterConstraintDefinitionSize,
            layout.GenericClassDefinitionSize,
            layout.GenericInstDefinitionSize,
            layout.Il2CppTypeDefinitionSize,
            layout.IndexSize,
            layout.PointerSize
        };
        if (sizes.Any(size => size <= 0))
            throw new ArgumentOutOfRangeException(nameof(profile), "All metadata layout sizes must be positive.");

        Il2CppTypeBitLayout typeBits = layout.Il2CppTypeBits;
        if (typeBits.ByReferenceMask == 0 || typeBits.PinnedMask == 0 ||
            (typeBits.ModifiersMask & typeBits.ByReferenceMask) != 0 ||
            (typeBits.ModifiersMask & typeBits.PinnedMask) != 0 ||
            (typeBits.ByReferenceMask & typeBits.PinnedMask) != 0) {
            throw new ArgumentOutOfRangeException(nameof(profile),
                "Il2CppType bit masks must be non-overlapping and include byref and pinned bits.");
        }
    }
}

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
            layout.ArrayTypeDefinitionSize,
            layout.IndexSize,
            layout.PointerSize,
            layout.Il2CppTypeRecord.Size,
            layout.Il2CppTypeRecord.DataSize,
            layout.Il2CppTypeRecord.ArrayDescriptorSize,
            layout.Il2CppTypeRecord.TypeOffset + 1,
            layout.Il2CppTypeRecord.FlagsOffset + 1
        };
        if (sizes.Any(size => size <= 0))
            throw new ArgumentOutOfRangeException(nameof(profile), "All metadata layout sizes must be positive.");

        Il2CppTypeRecordLayout typeRecord = layout.Il2CppTypeRecord;
        if (string.IsNullOrWhiteSpace(typeRecord.Name))
            throw new ArgumentOutOfRangeException(nameof(profile), "Il2CppType record layout must have a name.");
        if (typeRecord.DataSize is not (4 or 8))
            throw new ArgumentOutOfRangeException(nameof(profile), "Il2CppType data size must be 4 or 8.");
        if (typeRecord.AttrsOffset < typeRecord.DataSize ||
            typeRecord.TypeOffset <= typeRecord.AttrsOffset ||
            typeRecord.FlagsOffset <= typeRecord.TypeOffset ||
            typeRecord.FlagsOffset >= typeRecord.Size) {
            throw new ArgumentOutOfRangeException(nameof(profile),
                "Il2CppType field offsets must be increasing and fit in the record size.");
        }

        Il2CppTypeBitLayout typeBits = layout.Il2CppTypeBits;
        if (typeBits.ByReferenceMask == 0 || typeBits.PinnedMask == 0 ||
            (typeBits.ModifiersMask & typeBits.ByReferenceMask) != 0 ||
            (typeBits.ModifiersMask & typeBits.PinnedMask) != 0 ||
            (typeBits.ByReferenceMask & typeBits.PinnedMask) != 0) {
            throw new ArgumentOutOfRangeException(nameof(profile),
                "Il2CppType bit masks must be non-overlapping and include byref and pinned bits.");
        }

        ValidateRegistration(layout.Registration);
    }

    private static void ValidateRegistration(RegistrationLayout registration) {
        if (string.IsNullOrWhiteSpace(registration.Name))
            throw new ArgumentOutOfRangeException(nameof(registration), "Registration layout must have a name.");

        if (registration.CodeRegistrationSize <= 0 ||
            registration.MetadataRegistrationSize <= 0 ||
            registration.MetadataTablesSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(registration), "Registration blob sizes must be positive.");
        }

        RequirePointerInBlob(registration.MethodPointerOffset, registration.CodeRegistrationSize, "MethodPointer");
        RequireEncryptedInBlob(registration.TypeInfoCount, registration.MetadataRegistrationSize, "TypeInfoCount");
        RequirePointerInBlob(registration.GenericInstsOffset, registration.MetadataRegistrationSize, "GenericInsts");
        RequirePointerInBlob(registration.TypesRvaOffset, registration.MetadataRegistrationSize, "TypesRva");
        RequirePointerInBlob(registration.ArrayOffset, registration.MetadataRegistrationSize, "ArrayOffset");
        RequireInt32InBlob(registration.StringLiteralRvaOffset, registration.MetadataTablesSize, "StringLiteralRva");
        RequireEncryptedInBlob(registration.StringLiteralCount, registration.MetadataTablesSize, "StringLiteralCount");
    }

    private static void RequirePointerInBlob(int offset, int blobSize, string name) {
        if (offset < 0 || offset + 8 > blobSize)
            throw new ArgumentOutOfRangeException(nameof(offset), $"{name} offset 0x{offset:X} must fit an 8-byte pointer in a 0x{blobSize:X} blob.");
    }

    private static void RequireInt32InBlob(int offset, int blobSize, string name) {
        if (offset < 0 || offset + 4 > blobSize)
            throw new ArgumentOutOfRangeException(nameof(offset), $"{name} offset 0x{offset:X} must fit a 4-byte field in a 0x{blobSize:X} blob.");
    }

    private static void RequireEncryptedInBlob(EncryptedField field, int blobSize, string name) {
        RequireInt32InBlob(field.Offset, blobSize, name);
    }
}

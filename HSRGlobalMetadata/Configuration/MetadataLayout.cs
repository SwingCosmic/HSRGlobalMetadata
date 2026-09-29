namespace HSRGlobalMetadata.Configuration;

public sealed record Il2CppTypeBitLayout(
    byte ModifiersMask,
    byte ByReferenceMask,
    byte PinnedMask
) {
    public static Il2CppTypeBitLayout PackedPreV272 { get; } = new(
        ModifiersMask: 0x3F,
        ByReferenceMask: 0x40,
        PinnedMask: 0x80
    );
}

public sealed record Il2CppTypeRecordLayout(
    string Name,
    int Size,
    int DataSize,
    int AttrsOffset,
    int TypeOffset,
    int FlagsOffset,
    bool IndirectDataIsTypeIndex,
    int ArrayDescriptorSize
) {
    public static Il2CppTypeRecordLayout IndexBased8 { get; } = new(
        Name: "IndexBased8",
        Size: 8,
        DataSize: 4,
        AttrsOffset: 4,
        TypeOffset: 6,
        FlagsOffset: 7,
        IndirectDataIsTypeIndex: true,
        ArrayDescriptorSize: 32
    );

    public static Il2CppTypeRecordLayout VaBased16 { get; } = new(
        Name: "VaBased16",
        Size: 16,
        DataSize: 8,
        AttrsOffset: 8,
        TypeOffset: 10,
        FlagsOffset: 11,
        IndirectDataIsTypeIndex: false,
        ArrayDescriptorSize: 16
    );
}

public sealed record MetadataLayout(
    int MetadataHeaderSize,
    int TypeDefinitionSize,
    int ImageDefinitionSize,
    int MethodDefinitionSize,
    int FieldDefinitionSize,
    int PropertyDefinitionSize,
    int EventDefinitionSize,
    int ParameterDefinitionSize,
    int GenericContainerDefinitionSize,
    int GenericParameterDefinitionSize,
    int GenericFunctionDefinitionSize,
    int GenericParameterConstraintDefinitionSize,
    int GenericClassDefinitionSize,
    int GenericInstDefinitionSize,
    int IndexSize,
    int PointerSize,
    Il2CppTypeBitLayout Il2CppTypeBits,
    Il2CppTypeRecordLayout Il2CppTypeRecord,
    RegistrationLayout Registration
) {
    public int Il2CppTypeDefinitionSize => Il2CppTypeRecord.Size;
    public int ArrayTypeDefinitionSize => Il2CppTypeRecord.ArrayDescriptorSize;

    public static MetadataLayout Compose(
        Il2CppTypeRecordLayout typeRecord,
        RegistrationLayout registration,
        Il2CppTypeBitLayout? typeBits = null
    ) => new(
        MetadataHeaderSize: 0x208,
        TypeDefinitionSize: 70,
        ImageDefinitionSize: 40,
        MethodDefinitionSize: 26,
        FieldDefinitionSize: 8,
        PropertyDefinitionSize: 10,
        EventDefinitionSize: 14,
        ParameterDefinitionSize: 8,
        GenericContainerDefinitionSize: 16,
        GenericParameterDefinitionSize: 14,
        GenericFunctionDefinitionSize: 12,
        GenericParameterConstraintDefinitionSize: 4,
        GenericClassDefinitionSize: 8,
        GenericInstDefinitionSize: 16,
        IndexSize: 4,
        PointerSize: 8,
        Il2CppTypeBits: typeBits ?? Il2CppTypeBitLayout.PackedPreV272,
        Il2CppTypeRecord: typeRecord,
        Registration: registration
    );
}

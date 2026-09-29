namespace HSRGlobalMetadata.Configuration;

public sealed record Il2CppTypeBitLayout(
    byte ModifiersMask,
    byte ByReferenceMask,
    byte PinnedMask
);

public sealed record Il2CppTypeRecordLayout(
    int Size,
    int DataSize,
    int AttrsOffset,
    int TypeOffset,
    int FlagsOffset,
    bool IndirectDataIsTypeIndex
);

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
    int Il2CppTypeDefinitionSize,
    int ArrayTypeDefinitionSize,
    int IndexSize,
    int PointerSize,
    Il2CppTypeBitLayout Il2CppTypeBits,
    Il2CppTypeRecordLayout Il2CppTypeRecord
) {
    private static readonly Il2CppTypeBitLayout PackedPreV272Bits = new(
        ModifiersMask: 0x3F,
        ByReferenceMask: 0x40,
        PinnedMask: 0x80
    );

    public static MetadataLayout OspProdWin450 { get; } = new(
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
        Il2CppTypeDefinitionSize: 16,
        ArrayTypeDefinitionSize: 16,
        IndexSize: 4,
        PointerSize: 8,
        Il2CppTypeBits: PackedPreV272Bits,
        Il2CppTypeRecord: new Il2CppTypeRecordLayout(
            Size: 16,
            DataSize: 8,
            AttrsOffset: 8,
            TypeOffset: 10,
            FlagsOffset: 11,
            IndirectDataIsTypeIndex: false
        )
    );

    public static MetadataLayout OspProdWin460 { get; } = OspProdWin450 with {
        Il2CppTypeDefinitionSize = 8,
        ArrayTypeDefinitionSize = 32,
        Il2CppTypeRecord = new Il2CppTypeRecordLayout(
            Size: 8,
            DataSize: 4,
            AttrsOffset: 4,
            TypeOffset: 6,
            FlagsOffset: 7,
            IndirectDataIsTypeIndex: true
        )
    };
}

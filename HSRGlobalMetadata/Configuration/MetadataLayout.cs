namespace HSRGlobalMetadata.Configuration;

public sealed record Il2CppTypeBitLayout(
    byte ModifiersMask,
    byte ByReferenceMask,
    byte PinnedMask
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
    int IndexSize,
    int PointerSize,
    Il2CppTypeBitLayout Il2CppTypeBits
) {
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
        IndexSize: 4,
        PointerSize: 8,
        Il2CppTypeBits: new Il2CppTypeBitLayout(
            ModifiersMask: 0x3F,
            ByReferenceMask: 0x40,
            PinnedMask: 0x80
        )
    );
}

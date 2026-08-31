using Mono.Cecil;

namespace HSRGlobalMetadata.DummyDll.Model;

internal enum DummyTypeSignatureKind {
    Primitive,
    TypeDefinition,
    GenericInstance,
    Array,
    Pointer,
    ByReference,
    TypeGenericParameter,
    MethodGenericParameter,
    Unsupported
}

internal sealed record DummyTypeSignature(
    DummyTypeSignatureKind Kind,
    byte TypeCode,
    int DefinitionIndex = -1,
    int GenericParameterIndex = -1,
    int Rank = 0,
    DummyTypeSignature? ElementType = null,
    IReadOnlyList<DummyTypeSignature>? GenericArguments = null,
    string? Reason = null
) {
    public static DummyTypeSignature Primitive(byte typeCode) =>
        new(DummyTypeSignatureKind.Primitive, typeCode);

    public static DummyTypeSignature Definition(byte typeCode, int definitionIndex) =>
        new(DummyTypeSignatureKind.TypeDefinition, typeCode, DefinitionIndex: definitionIndex);

    public static DummyTypeSignature GenericInstance(
        int definitionIndex,
        IReadOnlyList<DummyTypeSignature> genericArguments
    ) => new(
        DummyTypeSignatureKind.GenericInstance,
        0x15,
        DefinitionIndex: definitionIndex,
        GenericArguments: genericArguments
    );

    public static DummyTypeSignature Array(byte typeCode, DummyTypeSignature elementType, int rank = 1) =>
        new(DummyTypeSignatureKind.Array, typeCode, Rank: rank, ElementType: elementType);

    public static DummyTypeSignature Pointer(DummyTypeSignature elementType) =>
        new(DummyTypeSignatureKind.Pointer, 0x0F, ElementType: elementType);

    public static DummyTypeSignature ByReference(DummyTypeSignature elementType) =>
        new(DummyTypeSignatureKind.ByReference, 0x10, ElementType: elementType);

    public static DummyTypeSignature GenericParameter(byte typeCode, int parameterIndex, bool method) =>
        new(
            method ? DummyTypeSignatureKind.MethodGenericParameter : DummyTypeSignatureKind.TypeGenericParameter,
            typeCode,
            GenericParameterIndex: parameterIndex
        );

    public static DummyTypeSignature Unsupported(byte typeCode, string reason) =>
        new(DummyTypeSignatureKind.Unsupported, typeCode, Reason: reason);
}

internal sealed record DummyGenericParameterModel(int MetadataIndex, string Name);

internal sealed record DummyImageModel(int Index, string Name, int TypeStart, int TypeCount);

internal sealed record DummyTypeModel(
    int Index,
    int ImageIndex,
    string Namespace,
    string Name,
    TypeAttributes Attributes,
    int DeclaringTypeIndex,
    IReadOnlyList<int> NestedTypeIndices,
    DummyTypeSignature? BaseType,
    IReadOnlyList<DummyTypeSignature> Interfaces,
    IReadOnlyList<DummyGenericParameterModel> GenericParameters,
    int FieldStart,
    int FieldCount,
    int MethodStart,
    int MethodCount,
    int PropertyStart,
    int PropertyCount
);

internal sealed record DummyFieldModel(
    int Index,
    string Name,
    FieldAttributes Attributes,
    DummyTypeSignature FieldType,
    uint? Offset,
    bool HasConstant,
    object? Constant
);

internal sealed record DummyParameterModel(
    int Index,
    string Name,
    ParameterAttributes Attributes,
    DummyTypeSignature ParameterType
);

internal sealed record DummyMethodModel(
    int Index,
    string Name,
    MethodAttributes Attributes,
    DummyTypeSignature ReturnType,
    IReadOnlyList<DummyGenericParameterModel> GenericParameters,
    IReadOnlyList<DummyParameterModel> Parameters
);

internal sealed record DummyPropertyModel(
    int Index,
    string Name,
    int? GetterMethodIndex,
    int? SetterMethodIndex
);

internal interface IDummyMetadataSource {
    int TypeCount { get; }
    IReadOnlyList<DummyImageModel> Images { get; }

    DummyTypeModel GetType(int typeDefinitionIndex);
    DummyFieldModel GetField(DummyTypeModel declaringType, int fieldOrdinal);
    DummyMethodModel GetMethod(DummyTypeModel declaringType, int absoluteMethodIndex);
    DummyPropertyModel GetProperty(DummyTypeModel declaringType, int propertyOrdinal);
}

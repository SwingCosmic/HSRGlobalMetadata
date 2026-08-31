using HSRGlobalMetadata.DummyDll.Generation;
using HSRGlobalMetadata.DummyDll.Model;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HSRGlobalMetadata.Tests;

public sealed class MetadataDummyAssemblyGeneratorTests {
    [Fact]
    public void GeneratesSerializableFieldGraphNestedTypesGenericsAndAutomaticProperty() {
        using var generator = new MetadataDummyAssemblyGenerator(new FixtureMetadataSource());

        AssemblyDefinition assembly = generator.Assemblies[1];
        TypeDefinition entity = Assert.Single(assembly.MainModule.Types, type => type.FullName == "Demo.Entity");
        TypeDefinition bucket = Assert.Single(assembly.MainModule.Types, type => type.FullName == "Demo.Bucket`1");

        Assert.Equal("Demo.Entity/Nested", Assert.Single(entity.NestedTypes).FullName);
        Assert.Equal("System.UInt32", entity.Fields.Single(field => field.Name == "Id").FieldType.FullName);
        Assert.Equal("System.String[]", entity.Fields.Single(field => field.Name == "Tags").FieldType.FullName);
        Assert.Equal(
            "Demo.Bucket`1<Demo.Entity>",
            entity.Fields.Single(field => field.Name == "Children").FieldType.FullName
        );
        Assert.Equal("T", bucket.Fields.Single(field => field.Name == "Value").FieldType.FullName);

        FieldDefinition id = entity.Fields.Single(field => field.Name == "Id");
        CustomAttribute offset = Assert.Single(id.CustomAttributes,
            attribute => attribute.AttributeType.Name == "FieldOffsetAttribute");
        Assert.Equal("0x10", Assert.Single(offset.Fields).Argument.Value);

        PropertyDefinition property = Assert.Single(entity.Properties);
        Assert.Equal("System.String", property.PropertyType.FullName);
        Assert.Equal(0, generator.Report.SyntheticBackingFieldCount);
        Assert.Contains(property.GetMethod!.Body.Instructions, instruction => instruction.OpCode == OpCodes.Ldfld);
        Assert.Contains(property.SetMethod!.Body.Instructions, instruction => instruction.OpCode == OpCodes.Stfld);
        Assert.Equal(0, generator.Report.PlaceholderTypeCount);
        Assert.Equal(4, generator.Report.SerializableFieldCount);
        Assert.Equal(0, generator.Report.SerializableFieldPlaceholderCount);

        using var stream = new MemoryStream();
        assembly.Write(stream);
        stream.Position = 0;
        using AssemblyDefinition reloaded = AssemblyDefinition.ReadAssembly(stream);
        TypeDefinition reloadedEntity = Assert.Single(reloaded.MainModule.Types,
            type => type.FullName == "Demo.Entity");
        Assert.Equal(4, reloadedEntity.Fields.Count);
        Assert.Single(reloadedEntity.Properties);
    }

    [Fact]
    public void UnsupportedTypeIsPreservedAsObjectAndReported() {
        var source = new FixtureMetadataSource(useUnsupportedIdType: true);
        using var generator = new MetadataDummyAssemblyGenerator(source);

        TypeDefinition entity = generator.Assemblies[1].MainModule.Types.Single(type => type.FullName == "Demo.Entity");
        Assert.Equal("System.Object", entity.Fields.Single(field => field.Name == "Id").FieldType.FullName);
        Assert.Equal(1, generator.Report.PlaceholderTypeCount);
        Assert.Equal(1, generator.Report.SerializableFieldPlaceholderCount);
        DummyDllDiagnostic diagnostic = Assert.Single(generator.Report.Diagnostics,
            item => item.Code == "TYPE_PLACEHOLDER");
        Assert.Equal("field", diagnostic.MemberKind);
        Assert.Equal(0, diagnostic.MemberIndex);
    }

    private sealed class FixtureMetadataSource : IDummyMetadataSource {
        private readonly bool _useUnsupportedIdType;
        private readonly DummyTypeModel[] _types;
        private readonly Dictionary<int, DummyFieldModel[]> _fields;
        private readonly Dictionary<int, DummyMethodModel> _methods;

        public int TypeCount => _types.Length;
        public IReadOnlyList<DummyImageModel> Images { get; } = [new(0, "Fixture.dll", 0, 3)];

        public FixtureMetadataSource(bool useUnsupportedIdType = false) {
            _useUnsupportedIdType = useUnsupportedIdType;
            _types = [
                new DummyTypeModel(
                    0, 0, "Demo", "Entity", TypeAttributes.Public, -1, [1], null, [], [],
                    0, 4, 0, 2, 0, 1
                ),
                new DummyTypeModel(
                    1, 0, "", "Nested", TypeAttributes.NestedPublic, 0, [], null, [], [],
                    4, 0, 2, 0, 1, 0
                ),
                new DummyTypeModel(
                    2, 0, "Demo", "Bucket`1", TypeAttributes.Public, -1, [], null, [],
                    [new DummyGenericParameterModel(100, "T")], 4, 1, 2, 0, 1, 0
                )
            ];

            _fields = new Dictionary<int, DummyFieldModel[]> {
                [0] = [
                    new DummyFieldModel(0, "Id", FieldAttributes.Public,
                        useUnsupportedIdType
                            ? DummyTypeSignature.Unsupported(0x1B, "Function pointers are deferred.")
                            : DummyTypeSignature.Primitive(0x09),
                        0x10, false, null),
                    new DummyFieldModel(1, "Tags", FieldAttributes.Public,
                        DummyTypeSignature.Array(0x1D, DummyTypeSignature.Primitive(0x0E)),
                        0x18, false, null),
                    new DummyFieldModel(2, "Children", FieldAttributes.Public,
                        DummyTypeSignature.GenericInstance(2, [DummyTypeSignature.Definition(0x12, 0)]),
                        0x20, false, null),
                    new DummyFieldModel(3, "<Name>k__BackingField", FieldAttributes.Private,
                        DummyTypeSignature.Primitive(0x0E), 0x28, false, null)
                ],
                [1] = [],
                [2] = [
                    new DummyFieldModel(4, "Value", FieldAttributes.Public,
                        DummyTypeSignature.GenericParameter(0x13, 100, method: false),
                        0x10, false, null)
                ]
            };

            _methods = new Dictionary<int, DummyMethodModel> {
                [0] = new DummyMethodModel(
                    0, "get_Name", MethodAttributes.Public | MethodAttributes.SpecialName,
                    DummyTypeSignature.Primitive(0x0E), [], []
                ),
                [1] = new DummyMethodModel(
                    1, "set_Name", MethodAttributes.Public | MethodAttributes.SpecialName,
                    DummyTypeSignature.Primitive(0x01), [],
                    [new DummyParameterModel(0, "value", ParameterAttributes.None,
                        DummyTypeSignature.Primitive(0x0E))]
                )
            };
        }

        public DummyTypeModel GetType(int typeDefinitionIndex) => _types[typeDefinitionIndex];

        public DummyFieldModel GetField(DummyTypeModel declaringType, int fieldOrdinal) =>
            _fields[declaringType.Index][fieldOrdinal];

        public DummyMethodModel GetMethod(DummyTypeModel declaringType, int absoluteMethodIndex) =>
            _methods[absoluteMethodIndex];

        public DummyPropertyModel GetProperty(DummyTypeModel declaringType, int propertyOrdinal) =>
            new(0, "Name", 0, 1);
    }
}

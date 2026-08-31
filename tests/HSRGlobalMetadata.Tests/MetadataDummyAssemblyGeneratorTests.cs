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
        Assert.Equal(6, generator.Report.MethodCount);
        Assert.Equal(6, generator.Report.PublicMethodCount);
        Assert.Equal(1, generator.Report.EventCount);
        Assert.Equal(1, generator.Report.PublicEventCount);
        Assert.Equal(4, generator.Report.AccessorCount);

        EventDefinition changed = Assert.Single(entity.Events);
        Assert.Equal("System.String", changed.EventType.FullName);
        Assert.Equal("add_Changed", changed.AddMethod!.Name);
        Assert.Equal("remove_Changed", changed.RemoveMethod!.Name);

        MethodDefinition tryRead = entity.Methods.Single(method => method.Name == "TryRead");
        Assert.IsType<ByReferenceType>(Assert.Single(tryRead.Parameters).ParameterType);
        CustomAttribute address = Assert.Single(tryRead.CustomAttributes,
            attribute => attribute.AttributeType.Name == "AddressAttribute");
        Assert.Equal("0x1234", address.Fields.Single(field => field.Name == "RVA").Argument.Value);
        Assert.Equal("0x234", address.Fields.Single(field => field.Name == "Offset").Argument.Value);
        Assert.Equal("0x180001234", address.Fields.Single(field => field.Name == "VA").Argument.Value);

        MethodDefinition echo = entity.Methods.Single(method => method.Name == "Echo");
        GenericParameter methodParameter = Assert.Single(echo.GenericParameters);
        Assert.Same(methodParameter, echo.ReturnType);
        Assert.Equal("Demo.Entity", Assert.Single(methodParameter.Constraints).ConstraintType.FullName);

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
        Assert.Equal(1, generator.Report.PublicMemberPlaceholderCount);
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
                    0, 4, 0, 6, 0, 1, 0, 1
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
                ),
                [2] = new DummyMethodModel(
                    2, "add_Changed", MethodAttributes.Public | MethodAttributes.SpecialName,
                    DummyTypeSignature.Primitive(0x01), [],
                    [new DummyParameterModel(1, "value", ParameterAttributes.None,
                        DummyTypeSignature.Primitive(0x0E))]
                ),
                [3] = new DummyMethodModel(
                    3, "remove_Changed", MethodAttributes.Public | MethodAttributes.SpecialName,
                    DummyTypeSignature.Primitive(0x01), [],
                    [new DummyParameterModel(2, "value", ParameterAttributes.None,
                        DummyTypeSignature.Primitive(0x0E))]
                ),
                [4] = new DummyMethodModel(
                    4, "TryRead", MethodAttributes.Public,
                    DummyTypeSignature.Primitive(0x02), [],
                    [new DummyParameterModel(3, "value", ParameterAttributes.Out,
                        DummyTypeSignature.ByReference(DummyTypeSignature.Primitive(0x0E)))],
                    new DummyMethodAddressModel(0x180001234, 0x1234, 0x234)
                ),
                [5] = new DummyMethodModel(
                    5, "Echo", MethodAttributes.Public,
                    DummyTypeSignature.GenericParameter(0x1E, 200, method: true),
                    [new DummyGenericParameterModel(200, "T", Constraints:
                        [DummyTypeSignature.Definition(0x12, 0)])],
                    []
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

        public DummyEventModel GetEvent(DummyTypeModel declaringType, int eventOrdinal) =>
            new(0, "Changed", DummyTypeSignature.Primitive(0x0E), 2, 3, null);
    }
}

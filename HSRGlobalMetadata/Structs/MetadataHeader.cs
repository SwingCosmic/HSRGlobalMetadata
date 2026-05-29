using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata.Structs;

public class MetadataHeader : MetadataBase {
    private static MetadataHeader? _instance;
    public static MetadataHeader Instance => _instance ?? throw new Exception("Not initialized");

    public const int MetadataHeaderSize = 0x208;
    
    [MetadataTag(0x20, MetadataOperation.SUB, 1292050039)]
    public int FieldsOffset { get; private set; }
    
    [MetadataTag(0x30, MetadataOperation.SUB, 587858498)]
    public int ParametersOffset { get; private set; }
    
    [MetadataTag(0x3C, MetadataOperation.SUB, 1752438875)]
    public int FieldAndParameterDefaultValueDataOffset { get; private set; }
    
    [MetadataTag(0x40, MetadataOperation.XOR, 0x3C685CDB)]
    public int PropertiesOffset { get; private set; }
    
    [MetadataTag(0x84, MetadataOperation.XOR, 0x68531D3F)]
    public int TypeDefinitionsOffset { get; private set; }
    
    [MetadataTag(0x9C, MetadataOperation.SUB, 1132541481)]
    public int IdxTableBaseOffset { get; private set; }
    
    [MetadataTag(0xEC, MetadataOperation.XOR, 0x67F701BA)]
    public int GenericMethodFunctionsDefsOffset { get; private set; }
    
    [MetadataTag(0xF0, MetadataOperation.SUB, 1656188401)]
    public int GenericContainerOffset { get; private set; }
    
    [MetadataTag(0x12C, MetadataOperation.XOR, 0x26F0FC20)]
    public int NestedTypesOffset { get; private set; }
    
    [MetadataTag(0x130, MetadataOperation.SUB, 22792381)]
    public int GenericParameterConstraintsOffset { get; private set; }
    
    [MetadataTag(0x134, MetadataOperation.XOR, 0x10210728)]
    public int ImagesSize { get; private set; }
    
    [MetadataTag(0x140, MetadataOperation.SUB, 626232567)]
    public int GenericParametersOffset { get; private set; }
    
    [MetadataTag(0x148, MetadataOperation.XOR, 0x329E1172)]
    public int FieldOffsetsOffset { get; private set; }
    
    [MetadataTag(0x14C, MetadataOperation.SUB, 207601004)]
    public int MethodOffset { get; private set; }
    
    [MetadataTag(0x150, MetadataOperation.SUB, 662544034)]
    public int ImagesOffset { get; private set; }
    
    [MetadataTag(0x158, MetadataOperation.SUB, 532747583)]
    public int AssemblyRelatedStuffOffset { get; private set; }
    
    [MetadataTag(0x164, MetadataOperation.XOR, 0x7F5C5934)]
    public int GenericClassOffset { get; private set; }
    
    [MetadataTag(0x180, MetadataOperation.SUB, 455350429)]
    public int TypeIndexMapOffset { get; private set; }
    
    [MetadataTag(0x1A8, MetadataOperation.XOR, 0x729B1A9E)]
    public int TypeDefinitionsSize { get; private set; }
    
    [MetadataTag(0x1AC, MetadataOperation.XOR, 0x37080D6E)]
    public int EventsOffset { get; private set; }
    
    [MetadataTag(0x1B4, MetadataOperation.SUB, 1924946706)]
    public int StringOffset { get; private set; }
    
    [MetadataTag(0x1C8, MetadataOperation.XOR, 0x42F9275)]
    public int InterfaceOffset { get; private set; }
    
    [MetadataTag(0x1E4, MetadataOperation.XOR, 0x720FEF70)]
    public int MethodSpecsSize { get; private set; }
    
    [MetadataTag(0x1FC, MetadataOperation.XOR, 0x6238CDB0)]
    public int FieldDefaultValuesOffset { get; private set; }

    public MetadataHeader(byte[] bytes) : base(bytes) {
        Populate();
    }

    public static void Initialize(string gameAssemblyPath) {
        var headerOffset = PatternScanner.FindPatternInFile(gameAssemblyPath, "4D 48 59 00");
        if (headerOffset == -1) {
            Console.WriteLine("Failed to find GlobalMetadataHeaderPointer in binary");
            return;
        }
        
        byte[] bytes = new ArraySegment<byte>(MetadataContext.Instance.GameAssembly, (int)headerOffset, MetadataHeaderSize).ToArray();

        _instance = new MetadataHeader(bytes);
    }
}
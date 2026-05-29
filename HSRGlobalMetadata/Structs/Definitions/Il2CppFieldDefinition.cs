using System.Text;
using HSRGlobalMetadata.Structs.Runtime;
using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata.Structs.Definitions;

public class Il2CppFieldDefinition : MetadataBase {
    private readonly int _index;
    private readonly int _fieldStart;
    private readonly int _fieldOffsetStart;

    public Il2CppFieldDefinition(int fieldStart, int fieldOffsetStart, int index) : base(MetadataContext.Instance.Metadata, MetadataHeader.Instance.FieldsOffset + index * 8) {
        _index = index;
        _fieldStart = fieldStart;
        _fieldOffsetStart = fieldOffsetStart;
        Populate();
    }
    
    [MetadataTag(0x00, MetadataOperation.ADD, 716162949)]
    public int NameIndex { get; private set; }
    
    [MetadataTag(0x04, MetadataOperation.ADD, 0)]
    public int TypeIndex { get; private set; }

    public uint Offset { get; private set; }

    public string Name;
    public Il2CppType Type;
    
    protected override void PostProcess() {
        var lcd = -1388221511 - 744344320 * (_index + 1954887780);

        Offset = BitConverter.ToUInt32(_bytes, MetadataHeader.Instance.FieldOffsetsOffset + (_index - _fieldStart + _fieldOffsetStart) * 4);
        TypeIndex += lcd;
        NameIndex += lcd;

        Name = StringProcessor.Decrypt(NameIndex);
        Type = Il2CppType.FromIndex(TypeIndex);
    }
    
    public object GetFieldStaticValue() {
        if (((FieldAttributes)Type.Attrs & FieldAttributes.Literal) == 0) return 0;
    
        if (!MetadataCache.FieldDefaultValues.TryGetValue(_index, out var entry))
            return 0;
        
        if (entry.typeIndex == -1) return 0;

        var type = Il2CppType.FromIndex(entry.typeIndex);
        var baseOffset = MetadataHeader.Instance.FieldAndParameterDefaultValueDataOffset + entry.dataOffset;
        var metadata = MetadataContext.Instance.Metadata;

        return type.Type switch {
            2 or 4 or 5 => metadata[baseOffset],
            3 or 6 or 7 => BitConverter.ToUInt16(metadata, baseOffset),
            8 or 9 or 17 or 0x2C => BitConverter.ToInt32(metadata, baseOffset),
            0x0C => BitConverter.ToSingle(metadata, baseOffset),
            0x0D => BitConverter.ToDouble(metadata, baseOffset),
            10 or 11 => BitConverter.ToInt64(metadata, baseOffset),
            14 => $"\"{Encoding.UTF8.GetString(metadata, baseOffset + 4, BitConverter.ToInt32(metadata, baseOffset))}\"",
            18 or 21 or 28 or 29 => BitConverter.ToInt32(metadata, baseOffset),
            _ => 0
        };
    }
}

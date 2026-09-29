using HSRGlobalMetadata.Structs.Definitions;
using HSRGlobalMetadata.Utils;
using System.Collections.Concurrent;

namespace HSRGlobalMetadata.Structs.Runtime;

public class Il2CppType {
    static readonly ConcurrentDictionary<int, string> GenericClassNameCache = new();

    static readonly Dictionary<byte, string> PrimitiveTypes = new() {
        [0x01] = "void",
        [0x02] = "bool",
        [0x03] = "char",
        [0x04] = "sbyte",
        [0x05] = "byte",
        [0x06] = "short",
        [0x07] = "ushort",
        [0x08] = "int",
        [0x09] = "uint",
        [0x0A] = "long",
        [0x0B] = "ulong",
        [0x0C] = "float",
        [0x0D] = "double",
        [0x0E] = "string",
        [0x16] = "TypedReference",
        [0x18] = "IntPtr",
        [0x19] = "UIntPtr",
        [0x1C] = "object",
    };
    
    public ulong Data;
    public ushort Attrs;
    public byte Type;
    public byte PackedFlags;
    public byte NumModifiers;
    public bool IsByReference;
    public bool IsPinned;
    public int Offset;

    public static ulong ImageBase => Configuration.RuntimeConfiguration.Current.ImageBase;
    
    private string _cachedName;

    public Il2CppType(int offset) {
        var bytes = MetadataContext.Instance.GameAssembly;
        var record = Configuration.RuntimeConfiguration.Current.Layout.Il2CppTypeRecord;
        Offset = offset;
        Data = record.DataSize == 8
            ? BitConverter.ToUInt64(bytes, offset)
            : BitConverter.ToUInt32(bytes, offset);
        Attrs = BitConverter.ToUInt16(bytes, offset + record.AttrsOffset);
        Type = bytes[offset + record.TypeOffset];
        PackedFlags = bytes[offset + record.FlagsOffset];
        (NumModifiers, IsByReference, IsPinned) = DecodePackedFlags(
            PackedFlags,
            Configuration.RuntimeConfiguration.Current.Layout.Il2CppTypeBits
        );
    }

    public static (byte NumModifiers, bool IsByReference, bool IsPinned) DecodePackedFlags(
        byte packedFlags,
        Configuration.Il2CppTypeBitLayout? layout = null
    ) {
        layout ??= Configuration.Il2CppTypeBitLayout.PackedPreV272;
        return (
            (byte)(packedFlags & layout.ModifiersMask),
            (packedFlags & layout.ByReferenceMask) != 0,
            (packedFlags & layout.PinnedMask) != 0
        );
    }

    public static Il2CppType FromIndex(int index) {
        if (index < 0 || index >= MetadataRegistration.Instance.TypeInfoCount) {
            throw new ArgumentOutOfRangeException($"{nameof(index)}, value: {index}"); 
        }
        if (MetadataCache.Types != null && index < MetadataCache.Types.Length && MetadataCache.Types[index] != null)
            return MetadataCache.Types[index];
        int offset = (int)PEHelper.RvaToOffset((uint)(MetadataRegistration.Instance.TypesRva +
            index * Configuration.RuntimeConfiguration.Current.Layout.Il2CppTypeDefinitionSize));
        
        return new Il2CppType(offset);
    }
    
    public string Name() {
        if (_cachedName != null) return _cachedName;
        _cachedName = ComputeName();
        return _cachedName;
    }

    public string ComputeName() {
        if (PrimitiveTypes.TryGetValue(Type, out var name))
            return name;

        switch (Type) {
            case 0x11:
            case 0x12:
                return ResolveTypeDefName((int)Data);
            
            case 0x15:
                int genericClassIndex = (int)Data;

                if (GenericClassNameCache.TryGetValue(genericClassIndex, out var cached))
                    return cached;

                int baseOffset = MetadataHeader.Instance.GenericClassOffset +
                    genericClassIndex * Configuration.RuntimeConfiguration.Current.Layout.GenericClassDefinitionSize;
                
                int instIndex = BitConverter.ToInt32(MetadataContext.Instance.StartupMetadata, baseOffset + 4);
                int typeDefIndex = BitConverter.ToInt32(MetadataContext.Instance.StartupMetadata, baseOffset);
                
                string openName = ResolveTypeDefName(typeDefIndex);
                int tick = openName.IndexOf('`');
                if (tick >= 0) openName = openName[..tick];

                if (instIndex == -1)
                    return openName;
                
                int instOffset = (int)PEHelper.RvaToOffset((uint)MetadataRegistration.Instance.GenericInstsOffset) +
                    instIndex * Configuration.RuntimeConfiguration.Current.Layout.GenericInstDefinitionSize;
                int argCount = BitConverter.ToInt32(MetadataContext.Instance.GameAssembly, instOffset);
                long arrayRva = BitConverter.ToInt64(MetadataContext.Instance.GameAssembly, instOffset + 8);

                int arrayOffset = (int)PEHelper.RvaToOffset((uint)((ulong)arrayRva - ImageBase));
                
                var args = new List<string>(argCount);

                for (int i = 0; i < argCount; i++) {
                    long ptr = BitConverter.ToInt64(MetadataContext.Instance.GameAssembly, arrayOffset + i * 8);
                    int typeArgOffset = (int)PEHelper.RvaToOffset((uint)((ulong)ptr - ImageBase));
                    
                    args.Add(new Il2CppType(typeArgOffset).Name());
                }

                string result = $"{openName}<{string.Join(", ", args)}>";
                GenericClassNameCache[genericClassIndex] = result;
                return result;
            
            case 0x0F:
                if (Data == 0) return "void*";
                return ResolveIndirect().Name() + "*";

            case 0x14:
                (int arrayElemOffset, int arrayRank) = ResolveArrayDescriptor();
                return $"{new Il2CppType(arrayElemOffset).Name()}[{new string(',', arrayRank - 1)}]";

            case 0x1D:
                return ResolveIndirect().Name() + "[]";

            case 0x10:
                return ResolveIndirect().Name();
            
            case 0x13:
            case 0x1E:
                int genericParamOffset = MetadataHeader.Instance.GenericParametersOffset +
                    (int)Data * Configuration.RuntimeConfiguration.Current.Layout.GenericParameterDefinitionSize;
                
                int nameIndex = BitConverter.ToInt32(MetadataContext.Instance.Metadata, genericParamOffset);
                int scramble = (int)(((ulong)(1252900171 *
                            ((((0x617FE3CC452CL * (ulong)Data + 0x9DC5DB71F0EB440L) >> 9)
                              + 718849585)
                             ^ 0x5278374D))) >> 15)
                            + 1149796643;

                int finalIndex = nameIndex - scramble;

                return StringProcessor.Decrypt(finalIndex);
            
            default:
                Console.WriteLine($"Unsupported type: {Type}, data: {Data:X}, RVA: 0x{PEHelper.OffsetToRva((ulong)Offset):X}");
                return "object";
        }
    }

    public Il2CppType ResolveIndirect() {
        if (Configuration.RuntimeConfiguration.Current.Layout.Il2CppTypeRecord.IndirectDataIsTypeIndex)
            return FromIndex(checked((int)Data));
        if (Data == 0)
            throw new InvalidDataException($"IL2CPP type at 0x{Offset:X} has a null indirect pointer.");
        return new Il2CppType(checked((int)PEHelper.RvaToOffset((uint)(Data - ImageBase))));
    }

    public (int ElementOffset, int Rank) ResolveArrayDescriptor() {
        var layout = Configuration.RuntimeConfiguration.Current.Layout;
        int entryOffset;
        if (layout.Il2CppTypeRecord.IndirectDataIsTypeIndex) {
            ulong rva = checked((ulong)MetadataRegistration.Instance.ArrayOffset + Data * (ulong)layout.ArrayTypeDefinitionSize);
            entryOffset = checked((int)PEHelper.RvaToOffset((uint)rva));
        }
        else {
            entryOffset = checked((int)PEHelper.RvaToOffset((uint)(Data - ImageBase)));
        }

        byte[] gameAssembly = MetadataContext.Instance.GameAssembly;
        long arrayElemPtr = BitConverter.ToInt64(gameAssembly, entryOffset);
        int arrayRank = gameAssembly[entryOffset + layout.PointerSize];
        if (arrayRank <= 0)
            throw new InvalidDataException($"IL2CPP array descriptor at 0x{entryOffset:X} has rank {arrayRank}.");
        int elementOffset = checked((int)PEHelper.RvaToOffset((uint)((ulong)arrayElemPtr - ImageBase)));
        return (elementOffset, arrayRank);
    }

    public static string ResolveTypeDefName(int typeDefinitionIndex) {
        Il2CppTypeDefinition typeDef = new Il2CppTypeDefinition(typeDefinitionIndex);

        string ns = typeDef.Namespace;
        string name = typeDef.Name;

        if (!string.IsNullOrEmpty(ns))
            return ns + "." + name;

        return name;
    }
}

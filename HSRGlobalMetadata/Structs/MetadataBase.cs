using System.Reflection;
using System.Collections.Concurrent;

namespace HSRGlobalMetadata.Utils;

public abstract class MetadataBase {
    protected byte[] _bytes;
    protected int _baseOffset;

    private static readonly ConcurrentDictionary<Type, (MetadataTagAttribute attr, PropertyInfo prop)[]> _cache = new();

    protected MetadataBase(byte[] bytes, int baseOffset = 0) {
        _bytes = bytes;
        _baseOffset = baseOffset;
    }

    protected void Populate() {
        var type = GetType();
        if (!_cache.TryGetValue(type, out var entries)) {
            entries = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (attr: p.GetCustomAttribute<MetadataTagAttribute>(), prop: p))
                .Where(x => x.attr != null)
                .Select(x => (x.attr!, x.prop))
                .ToArray();
            _cache.TryAdd(type, entries);
        }

        foreach (var (attr, prop) in entries) {
            var propType = prop.PropertyType;
    
            long raw = propType switch {
                _ when propType == typeof(byte) => _bytes[_baseOffset + attr.Offset],
                _ when propType == typeof(int) => BitConverter.ToInt32 (_bytes, _baseOffset + attr.Offset),
                _ when propType == typeof(uint) => BitConverter.ToUInt32(_bytes, _baseOffset + attr.Offset),
                _ when propType == typeof(long) => BitConverter.ToInt64 (_bytes, _baseOffset + attr.Offset),
                _ when propType == typeof(ulong) => (long)BitConverter.ToUInt64(_bytes, _baseOffset + attr.Offset),
                _ when propType == typeof(short) => BitConverter.ToInt16 (_bytes, _baseOffset + attr.Offset),
                _ when propType == typeof(ushort) => BitConverter.ToUInt16(_bytes, _baseOffset + attr.Offset),
                _ => throw new NotSupportedException($"Unsupported metadata type: {propType}")
            };

            prop.SetValue(this, ApplyOperation(attr.Operation, raw, attr.Operand, propType));
        }

        PostProcess();
    }
    
    private static object ApplyOperation(MetadataOperation op, long raw, long operand, Type targetType) {
        long result = op switch {
            MetadataOperation.XOR => raw ^ operand,
            MetadataOperation.SUB => raw - operand,
            MetadataOperation.ADD => raw + operand,
            _ => raw
        };

        return targetType switch {
            _ when targetType == typeof(byte) => (byte)result,
            _ when targetType == typeof(int) => (int)result,
            _ when targetType == typeof(uint) => (uint)result,
            _ when targetType == typeof(long) => result,
            _ when targetType == typeof(ulong) => (ulong)result,
            _ when targetType == typeof(short) => (short)result,
            _ when targetType == typeof(ushort) => (ushort)result,
            _ => throw new NotSupportedException($"Unsupported metadata type: {targetType}")
        };
    }

    protected virtual void PostProcess() {}
}

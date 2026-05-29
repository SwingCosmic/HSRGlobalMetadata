using HSRGlobalMetadata.Structs;

namespace HSRGlobalMetadata.Utils;

using System;
using System.Text;
using System.Collections.Concurrent;

public static class StringProcessor {
    static readonly ConcurrentDictionary<int, string> StringCache = new();

    const ulong ScalarStep = 0x3E693CD23A41FDEFUL;

    static ulong DeriveKey(int index) {
        uint andKey = (index < 0) ? 0x7FFFFFu : 0x1FFFFFFu;
        ulong ski = (ulong)(index & (int)andKey);

        return unchecked(0x907C49622D94D21AUL * ski + 0x75B679DAF67C3F24UL);
    }

    public static string Decrypt(int index) {
        if (StringCache.TryGetValue(index, out var cached))
            return cached;

        uint uIndex = (uint)index;

        int stringLength = (int)((uIndex >> 25) & 0x3F);

        if (index < 0)
            stringLength = (int)((uIndex >> 23) & 0xFF);

        if (stringLength <= 0 || index == -1)
            return "";
        
        uint andKey = (index < 0) ? 0x7FFFFFu : 0x1FFFFFFu;
        int stringKeyIndex = index & (int)andKey;
        ulong xorKey = DeriveKey(index);
        int dataOffset = MetadataHeader.Instance.StringOffset + stringKeyIndex;
        int stringBlocks = (stringLength + 7) >> 3;

        byte[] outBuf = new byte[stringLength + 1];

        for (int i = 0; i < stringBlocks; i++) {
            ulong enc = BitConverter.ToUInt64(MetadataContext.Instance.Metadata,dataOffset + i * 8);
            ulong dec = enc ^ xorKey;
            xorKey = unchecked(xorKey + ScalarStep);

            int writeLen = Math.Min(8, stringLength - i * 8);
            Buffer.BlockCopy(BitConverter.GetBytes(dec), 0, outBuf, i * 8, writeLen);
        }

        string result = Encoding.UTF8.GetString(outBuf, 0, stringLength);
        StringCache.TryAdd(index, result);
        return result;
    }
}
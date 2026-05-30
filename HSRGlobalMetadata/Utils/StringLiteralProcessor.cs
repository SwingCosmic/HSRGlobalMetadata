namespace HSRGlobalMetadata.Utils;

using System.Runtime.CompilerServices;

public static class StringLiteralProcessor {
    private const ulong KeyMultiplier = 0xDE8C09C836133DBDUL;
    private const ulong KeyXorMask = 0x18D025C96EE74E86UL;
    private const ulong KeyAddOffset = 0x2C6833CC6F0A9C48UL;
    private const ulong BlockStep = 0x464C46540F730312UL;
    private const ulong SimdStep1 = 0x8C988CA81EE60624UL;
    private const ulong SimdStep2 = 0x193119503DCC0C48UL;
    private const ulong SimdStep3 = 0xA5C9A5F85CB2126CUL;
    private const ulong SimdStep4 = 0x326232A07B981890UL;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe byte[] Decrypt(uint index, byte[] metadataBytes, uint startPtr, uint length) {
        if (length == 0)
            return Array.Empty<byte>();

        fixed (byte* srcPtr = &metadataBytes[startPtr]) {
            int allocSize = ((int)length + 7) & ~7;
            byte[] buffer = new byte[allocSize];

            fixed (byte* outPtr = buffer) {
                ulong key = unchecked((KeyMultiplier * index) ^ KeyXorMask) + KeyAddOffset;
                ulong chunkCount = (length + 7) >> 3;
                ulong chunkCountMinus1 = unchecked(chunkCount - 1UL);

                if (chunkCountMinus1 < 3) {
                    for (ulong i = 0; i < chunkCount; i++) {
                        ulong srcVal = *(ulong*)(srcPtr + 8 * i);
                        *(ulong*)(outPtr + 8 * i) = key ^ srcVal;
                        key = unchecked(key + BlockStep);
                    }
                    return SliceBuffer(buffer, length);
                }

                ulong keyLo = key;
                ulong keyHi = unchecked(key + BlockStep);
                ulong alignedChunkCount = chunkCount & ~3UL;
                ulong unrollPasses = unchecked(((alignedChunkCount - 4UL) >> 2) + 1UL);
                ulong offset = 0;

                if (alignedChunkCount == 4UL) {
                    if ((unrollPasses & 1) != 0) {
                        Xor16(outPtr + offset, srcPtr + offset, keyLo, keyHi);
                        Xor16(outPtr + offset + 16, srcPtr + offset + 16, unchecked(keyLo + SimdStep1), unchecked(keyHi + SimdStep1));
                    }
                } else {
                    ulong passes = unrollPasses & ~1UL;

                    while (passes != 0) {
                        Xor16(outPtr + offset, srcPtr + offset, keyLo, keyHi);
                        Xor16(outPtr + offset + 16, srcPtr + offset + 16, unchecked(keyLo + SimdStep1), unchecked(keyHi + SimdStep1));
                        Xor16(outPtr + offset + 32, srcPtr + offset + 32, unchecked(keyLo + SimdStep2), unchecked(keyHi + SimdStep2));
                        Xor16(outPtr + offset + 48, srcPtr + offset + 48, unchecked(keyLo + SimdStep3), unchecked(keyHi + SimdStep3));

                        keyLo = unchecked(keyLo + SimdStep4);
                        keyHi = unchecked(keyHi + SimdStep4);
                        offset = unchecked(offset + 64);
                        passes = unchecked(passes - 2);
                    }

                    if ((unrollPasses & 1) != 0) {
                        Xor16(outPtr + offset, srcPtr + offset, keyLo, keyHi);
                        Xor16(outPtr + offset + 16, srcPtr + offset + 16, unchecked(keyLo + SimdStep1), unchecked(keyHi + SimdStep1));
                    }
                }

                if (chunkCount == alignedChunkCount)
                    return SliceBuffer(buffer, length);

                key = unchecked(key + BlockStep * alignedChunkCount);

                ulong tailOffset = 8UL * alignedChunkCount;
                ulong tailChunks = unchecked(chunkCount - alignedChunkCount);

                for (ulong i = 0; i < tailChunks; i++) {
                    ulong srcVal = *(ulong*)(srcPtr + tailOffset + 8 * i);
                    *(ulong*)(outPtr + tailOffset + 8 * i) = key ^ srcVal;
                    key = unchecked(key + BlockStep);
                }

                return SliceBuffer(buffer, length);
            }
        }
    }

    private static unsafe void Xor16(byte* dst, byte* src, ulong keyLo, ulong keyHi) {
        *(ulong*)dst = *(ulong*)src ^ keyLo;
        *(ulong*)(dst + 8) = *(ulong*)(src + 8) ^ keyHi;
    }

    private static byte[] SliceBuffer(byte[] buffer, uint length) {
        byte[] result = new byte[length];
        Buffer.BlockCopy(buffer, 0, result, 0, (int)length);
        return result;
    }
}

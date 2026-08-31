using HSRGlobalMetadata.Structs.Runtime;

namespace HSRGlobalMetadata.Tests;

public sealed class Il2CppTypeTests {
    [Theory]
    [InlineData(0x00, 0, false, false)]
    [InlineData(0x3F, 63, false, false)]
    [InlineData(0x40, 0, true, false)]
    [InlineData(0x80, 0, false, true)]
    [InlineData(0xC5, 5, true, true)]
    public void PackedPreV272TypeFlagsAreDecoded(
        byte packed,
        byte expectedModifiers,
        bool expectedByReference,
        bool expectedPinned
    ) {
        var decoded = Il2CppType.DecodePackedFlags(packed);

        Assert.Equal(expectedModifiers, decoded.NumModifiers);
        Assert.Equal(expectedByReference, decoded.IsByReference);
        Assert.Equal(expectedPinned, decoded.IsPinned);
    }
}

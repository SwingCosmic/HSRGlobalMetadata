using HSRGlobalMetadata.Configuration;

namespace HSRGlobalMetadata.Tests;

public sealed class RuntimeConfigurationTests {
    [Fact]
    public void DefaultProfileContainsVerifiedSampleLayout() {
        VersionProfile profile = RuntimeConfiguration.GetDefault();

        Assert.Equal("OSPRODWin4.6.0", profile.Name);
        Assert.Equal(0x0059484Du, profile.MetadataMagic);
        Assert.Equal(0x180000000ul, profile.ImageBase);
        Assert.Equal(70, profile.Layout.TypeDefinitionSize);
        Assert.Equal(40, profile.Layout.ImageDefinitionSize);
        Assert.Equal(26, profile.Layout.MethodDefinitionSize);
        Assert.Equal(8, profile.Layout.Il2CppTypeDefinitionSize);
        Assert.Equal(4, profile.Layout.Il2CppTypeRecord.DataSize);
        Assert.True(profile.Layout.Il2CppTypeRecord.IndirectDataIsTypeIndex);
    }

    [Fact]
    public void OspProdWin450ProfileKeepsSixteenByteIl2CppTypeRecords() {
        Assert.True(VersionProfiles.TryGet("OSPRODWin4.5.0", out VersionProfile profile));
        Assert.Equal(16, profile.Layout.Il2CppTypeDefinitionSize);
        Assert.Equal(8, profile.Layout.Il2CppTypeRecord.DataSize);
        Assert.False(profile.Layout.Il2CppTypeRecord.IndirectDataIsTypeIndex);
    }
}

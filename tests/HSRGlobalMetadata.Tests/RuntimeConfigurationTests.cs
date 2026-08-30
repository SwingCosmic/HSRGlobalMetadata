using HSRGlobalMetadata.Configuration;

namespace HSRGlobalMetadata.Tests;

public sealed class RuntimeConfigurationTests {
    [Fact]
    public void DefaultProfileContainsVerifiedSampleLayout() {
        VersionProfile profile = RuntimeConfiguration.GetDefault();

        Assert.Equal("OSPRODWin4.5.0", profile.Name);
        Assert.Equal(0x0059484Du, profile.MetadataMagic);
        Assert.Equal(0x180000000ul, profile.ImageBase);
        Assert.Equal(70, profile.Layout.TypeDefinitionSize);
        Assert.Equal(40, profile.Layout.ImageDefinitionSize);
        Assert.Equal(26, profile.Layout.MethodDefinitionSize);
    }
}

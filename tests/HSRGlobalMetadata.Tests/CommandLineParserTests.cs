using HSRGlobalMetadata.Configuration;

namespace HSRGlobalMetadata.Tests;

public sealed class CommandLineParserTests {
    [Fact]
    public void PositionalDirectoryPreservesLegacyOutputs() {
        CommandLineOptions options = CommandLineParser.Parse(["game"]);

        Assert.Equal("game", options.GameDirectory);
        Assert.Equal(VersionProfiles.DefaultName, options.Version);
        Assert.True(options.GenerateDump);
        Assert.True(options.GenerateStringLiterals);
        Assert.False(options.GenerateDummyDll);
    }

    [Fact]
    public void DummyDllAndOverridesAreParsed() {
        CommandLineOptions options = CommandLineParser.Parse([
            "game",
            "--dummy-dll",
            "--output", "artifacts",
            "--version", "OSPRODWin4.5.0",
            "--metadata-magic", "0x0059484D",
            "--image-base", "6442450944",
            "--strict"
        ]);

        Assert.True(options.GenerateDummyDll);
        Assert.True(options.Strict);
        Assert.Equal("artifacts", options.OutputDirectory);
        Assert.Equal(0x0059484Du, options.MetadataMagicOverride);
        Assert.Equal(0x180000000ul, options.ImageBaseOverride);

        VersionProfile profile = options.ResolveProfile();
        Assert.Equal(0x0059484Du, profile.MetadataMagic);
        Assert.Equal(0x180000000ul, profile.ImageBase);
    }

    [Fact]
    public void DuplicateOptionIsRejected() {
        CommandLineException error = Assert.Throws<CommandLineException>(() =>
            CommandLineParser.Parse(["game", "--dummy-dll", "--dummy-dll"])
        );

        Assert.Contains("more than once", error.Message);
    }

    [Fact]
    public void UnknownOptionIsRejected() {
        CommandLineException error = Assert.Throws<CommandLineException>(() =>
            CommandLineParser.Parse(["game", "--unknown"])
        );

        Assert.Contains("Unknown option", error.Message);
    }

    [Fact]
    public void UnknownVersionIsRejectedWhenProfileIsResolved() {
        CommandLineOptions options = CommandLineParser.Parse(["game", "--version", "future"]);

        CommandLineException error = Assert.Throws<CommandLineException>(() => options.ResolveProfile());
        Assert.Contains("Unknown version", error.Message);
    }

    [Theory]
    [InlineData("4294967296", "exceeds")]
    [InlineData("0x", "not a valid")]
    [InlineData("wat", "not a valid")]
    public void InvalidMetadataMagicIsRejected(string value, string expectedMessage) {
        CommandLineException error = Assert.Throws<CommandLineException>(() =>
            CommandLineParser.Parse(["game", "--metadata-magic", value])
        );

        Assert.Contains(expectedMessage, error.Message);
    }
}

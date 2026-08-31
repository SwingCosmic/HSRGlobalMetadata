using HSRGlobalMetadata.DummyDll.Generation;
using HSRGlobalMetadata.Structs.Definitions;
using Mono.Cecil;

namespace HSRGlobalMetadata.Tests;

public sealed class DummyDllTemplateTests {
    [Fact]
    public void EmbeddedTemplateContainsRequiredAttributes() {
        byte[] bytes = EmbeddedDummyDllTemplate.Read();

        Assert.NotEmpty(bytes);
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(bytes));
        string[] names = assembly.MainModule.Types.Select(type => type.Name).ToArray();
        Assert.Contains("AddressAttribute", names);
        Assert.Contains("FieldOffsetAttribute", names);
        Assert.Contains("MetadataOffsetAttribute", names);
        Assert.Contains("TokenAttribute", names);
    }

    [Fact]
    public void GeneratorWithNoImagesStillProducesTemplateAssembly() {
        using var generator = new BlankDummyAssemblyGenerator(Array.Empty<Il2CppImageDefinition>());

        AssemblyDefinition assembly = Assert.Single(generator.Assemblies);
        Assert.Equal("Il2CppDummyDll.dll", assembly.MainModule.Name);
    }

    [Theory]
    [InlineData("Assembly-CSharp.dll", "Assembly-CSharp.dll")]
    [InlineData("Game", "Game.dll")]
    [InlineData("folder\\Nested.dll", "Nested.dll")]
    public void ImageNamesAreNormalized(string input, string expected) {
        Assert.Equal(expected, BlankDummyAssemblyGenerator.NormalizeModuleName(input));
    }
}

using HSRGlobalMetadata.Configuration;

namespace HSRGlobalMetadata.Tests;

[Collection("RuntimeConfiguration")]
public sealed class VersionCompositionTests {
    [Theory]
    [InlineData(VersionProfiles.OsProdWin430Name, "IndexBased8", "OsProdWin440")]
    [InlineData(VersionProfiles.OsProdWin440Name, "VaBased16", "OsProdWin440")]
    [InlineData(VersionProfiles.OsProdWin450Name, "VaBased16", "OsProdWin450")]
    [InlineData(VersionProfiles.OsProdWin460Name, "IndexBased8", "OsProdWin450")]
    public void BuiltInProfilesComposeIndependentModes(string version, string typeRecord, string registration) {
        Assert.True(VersionProfiles.TryGet(version, out VersionProfile profile));
        Assert.Equal(typeRecord, profile.Layout.Il2CppTypeRecord.Name);
        Assert.Equal(registration, profile.Layout.Registration.Name);
    }

    [Fact]
    public void FourThreeAndFourSixShareTheIndexBasedTypeRecord() {
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin430Name, out VersionProfile v43));
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin460Name, out VersionProfile v46));
        Assert.Same(v43.Layout.Il2CppTypeRecord, v46.Layout.Il2CppTypeRecord);
        Assert.Same(Il2CppTypeRecordLayout.IndexBased8, v43.Layout.Il2CppTypeRecord);
    }

    [Fact]
    public void FourFourAndFourFiveShareTheVaBasedTypeRecord() {
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin440Name, out VersionProfile v44));
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin450Name, out VersionProfile v45));
        Assert.Same(v44.Layout.Il2CppTypeRecord, v45.Layout.Il2CppTypeRecord);
        Assert.Same(Il2CppTypeRecordLayout.VaBased16, v44.Layout.Il2CppTypeRecord);
    }

    [Fact]
    public void FourThreeAndFourFourShareRegistrationSlots() {
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin430Name, out VersionProfile v43));
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin440Name, out VersionProfile v44));
        Assert.Same(RegistrationLayout.OsProdWin440, v43.Layout.Registration);
        Assert.Same(v43.Layout.Registration, v44.Layout.Registration);
        Assert.Equal(0x88, v43.Layout.Registration.MethodPointerOffset);
        Assert.Equal(0x80, v43.Layout.Registration.TypeInfoCount.Offset);
        Assert.Equal(458010256, v43.Layout.Registration.TypeInfoCount.Operand);
        Assert.Equal(0x20, v43.Layout.Registration.GenericInstsOffset);
        Assert.Equal(0x68, v43.Layout.Registration.TypesRvaOffset);
        Assert.Equal(0x90, v43.Layout.Registration.ArrayOffset);
        Assert.Equal(0x28, v43.Layout.Registration.StringLiteralRvaOffset);
        Assert.Equal(0x40, v43.Layout.Registration.StringLiteralCount.Offset);
    }

    [Fact]
    public void FourFiveAndFourSixShareRegistrationSlots() {
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin450Name, out VersionProfile v45));
        Assert.True(VersionProfiles.TryGet(VersionProfiles.OsProdWin460Name, out VersionProfile v46));
        Assert.Same(RegistrationLayout.OsProdWin450, v45.Layout.Registration);
        Assert.Same(v45.Layout.Registration, v46.Layout.Registration);
        Assert.Equal(0x40, v45.Layout.Registration.MethodPointerOffset);
        Assert.Equal(0x48, v45.Layout.Registration.TypeInfoCount.Offset);
        Assert.Equal(1455078204, v45.Layout.Registration.TypeInfoCount.Operand);
        Assert.Equal(0x38, v45.Layout.Registration.GenericInstsOffset);
        Assert.Equal(0x80, v45.Layout.Registration.TypesRvaOffset);
        Assert.Equal(0x70, v45.Layout.Registration.ArrayOffset);
        Assert.Equal(0x10, v45.Layout.Registration.StringLiteralRvaOffset);
        Assert.Equal(0x2C, v45.Layout.Registration.StringLiteralCount.Offset);
    }

    [Fact]
    public void StringLiteralCountXorIsSharedAcrossRegistrationLayouts() {
        Assert.Equal(RegistrationLayout.SharedStringLiteralCountXor, RegistrationLayout.OsProdWin440.StringLiteralCount.Operand);
        Assert.Equal(RegistrationLayout.SharedStringLiteralCountXor, RegistrationLayout.OsProdWin450.StringLiteralCount.Operand);
        Assert.Equal(MetadataOperation.XOR, RegistrationLayout.OsProdWin440.StringLiteralCount.Operation);
        Assert.Equal(MetadataOperation.XOR, RegistrationLayout.OsProdWin450.StringLiteralCount.Operation);
    }

    [Fact]
    public void EncryptedFieldAppliesSubAndXor() {
        Assert.Equal(10, EncryptedField.Sub(0, 5).Apply(15));
        Assert.Equal(0x10, EncryptedField.Xor(0, 0x0F).Apply(0x1F));

        byte[] bytes = BitConverter.GetBytes(1455078204 + 12345);
        Assert.Equal(12345, EncryptedField.Sub(0, 1455078204).DecodeInt32(bytes));

        byte[] xored = BitConverter.GetBytes(0xBD08DC8 ^ 77);
        Assert.Equal(77, EncryptedField.Xor(0, RegistrationLayout.SharedStringLiteralCountXor).DecodeInt32(xored));
    }

    [Fact]
    public void CreateMixesExistingModesWithoutANewLayoutType() {
        VersionProfile mixed = VersionProfiles.Create(
            "future-mix",
            Il2CppTypeRecordLayout.IndexBased8,
            RegistrationLayout.OsProdWin440
        );

        VersionProfile previous = RuntimeConfiguration.Current;
        try {
            RuntimeConfiguration.Initialize(mixed);
            Assert.Equal("future-mix", RuntimeConfiguration.Current.Name);
            Assert.Same(Il2CppTypeRecordLayout.IndexBased8, mixed.Layout.Il2CppTypeRecord);
            Assert.Same(RegistrationLayout.OsProdWin440, mixed.Layout.Registration);
            Assert.Equal(70, mixed.Layout.TypeDefinitionSize);
            Assert.Equal(8, mixed.Layout.Il2CppTypeDefinitionSize);
            Assert.Equal(32, mixed.Layout.ArrayTypeDefinitionSize);
        }
        finally {
            RuntimeConfiguration.Initialize(previous);
        }
    }
}

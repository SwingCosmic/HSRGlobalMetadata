using System.Buffers.Binary;
using HSRGlobalMetadata.Configuration;

namespace HSRGlobalMetadata.Structs;

public class MetadataContext {
    public byte[] Metadata { get; private set; } = null!;
    public byte[] StartupMetadata { get; private set; } = null!;
    public byte[] GameAssembly { get; private set; } = null!;

    private static MetadataContext? _instance;
    public static MetadataContext Instance => _instance ?? throw new Exception("MetadataContext is not initialized");

    public static void Initialize(string metadataPath, string startupPath, string gameAssemblyPath) {
        byte[] raw = File.ReadAllBytes(metadataPath);
        VersionProfile profile = RuntimeConfiguration.Current;
        int headerSize = profile.Layout.MetadataHeaderSize;
        if (raw.Length < headerSize)
            throw new InvalidDataException($"global-metadata.dat is smaller than the configured header size ({headerSize} bytes).");

        uint actualMagic = BinaryPrimitives.ReadUInt32LittleEndian(raw);
        if (actualMagic != profile.MetadataMagic) {
            throw new InvalidDataException(
                $"global-metadata.dat magic mismatch. Expected {profile.MetadataMagicText}, got 0x{actualMagic:X8}. " +
                "Select the correct --version or provide --metadata-magic."
            );
        }

        var metadata = new byte[raw.Length - headerSize];
        Buffer.BlockCopy(raw, headerSize, metadata, 0, metadata.Length);

        _instance = new MetadataContext {
            Metadata = metadata,
            StartupMetadata = File.ReadAllBytes(startupPath),
            GameAssembly = File.ReadAllBytes(gameAssemblyPath)
        };
    }
}

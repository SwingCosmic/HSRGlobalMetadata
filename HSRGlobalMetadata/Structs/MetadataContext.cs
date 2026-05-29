namespace HSRGlobalMetadata.Structs;

public class MetadataContext {
    public byte[] Metadata { get; private set; } = null!;
    public byte[] StartupMetadata { get; private set; } = null!;
    public byte[] GameAssembly { get; private set; } = null!;

    private static MetadataContext? _instance;
    public static MetadataContext Instance => _instance ?? throw new Exception("MetadataContext is not initialized");

    public static void Initialize(string metadataPath, string startupPath, string gameAssemblyPath) {
        var raw = File.ReadAllBytes(metadataPath);

        var metadata = new byte[raw.Length - MetadataHeader.MetadataHeaderSize];
        Buffer.BlockCopy(raw, MetadataHeader.MetadataHeaderSize, metadata, 0, metadata.Length);

        _instance = new MetadataContext {
            Metadata = metadata,
            StartupMetadata = File.ReadAllBytes(startupPath),
            GameAssembly = File.ReadAllBytes(gameAssemblyPath)
        };
    }
}
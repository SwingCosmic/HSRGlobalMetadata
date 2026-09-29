using HSRGlobalMetadata.Configuration;
using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata.Structs;

public class MetadataRegistration : MetadataBase {
    private static MetadataRegistration? _instance;
    public static MetadataRegistration Instance => _instance ?? throw new Exception("Not initialized");

    public MetadataRegistration(byte[] bytes) : base(bytes) {
        Populate();
    }
    
    public static void Initialize(string gameAssemblyPath) {
        RegistrationLayout registration = RuntimeConfiguration.Current.Layout.Registration;
        long metadataRegistrationPtr = RegisterPointersFunction.Initialize(gameAssemblyPath).GetMetadataRegistration();
        byte[] bytes = new ArraySegment<byte>(MetadataContext.Instance.GameAssembly, (int)metadataRegistrationPtr, registration.MetadataRegistrationSize).ToArray();
        _instance = new MetadataRegistration(bytes);
    }

    private long ToFileOffset(long va) => va - checked((long)RuntimeConfiguration.Current.ImageBase);
    private long ReadPtr(int offset) => ToFileOffset(BitConverter.ToInt64(_bytes, offset));

    public int TypeInfoCount { get; set; }
    
    public long TypesRva { get; private set; }
    public long GenericInstsOffset { get; private set; }
    public long ArrayOffset { get; set; }
    
    protected override void PostProcess() {
        RegistrationLayout registration = RuntimeConfiguration.Current.Layout.Registration;
        TypeInfoCount = registration.TypeInfoCount.DecodeInt32(_bytes);
        GenericInstsOffset = ReadPtr(registration.GenericInstsOffset);
        TypesRva = ReadPtr(registration.TypesRvaOffset);
        ArrayOffset = ReadPtr(registration.ArrayOffset);
    }
}

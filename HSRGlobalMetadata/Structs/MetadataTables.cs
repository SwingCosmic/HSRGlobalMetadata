using HSRGlobalMetadata.Configuration;
using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata.Structs;

public class MetadataTables: MetadataBase {
  private static MetadataTables? _instance;
  public static MetadataTables Instance => _instance ?? throw new Exception("MetadataTables not initialized");

  public int StringLiteralRva { get; private set; }

  public int StringLiteralCount { get; private set; }

  public MetadataTables(byte[] bytes): base(bytes) {
    Populate();
  }

  public static void Initialize(string gameAssemblyPath) {
    RegistrationLayout registration = RuntimeConfiguration.Current.Layout.Registration;
    long metadataTablesPtr = RegisterPointersFunction.Initialize(gameAssemblyPath).GetMetadataTables();
    byte[] bytes = new ArraySegment<byte>(MetadataContext.Instance.GameAssembly, (int)metadataTablesPtr, registration.MetadataTablesSize).ToArray();
    _instance = new MetadataTables(bytes);
  }

  protected override void PostProcess() {
    RegistrationLayout registration = RuntimeConfiguration.Current.Layout.Registration;
    StringLiteralRva = BitConverter.ToInt32(_bytes, registration.StringLiteralRvaOffset);
    StringLiteralCount = registration.StringLiteralCount.DecodeInt32(_bytes);
    StringLiteralRva = unchecked(StringLiteralRva - (int)RuntimeConfiguration.Current.ImageBase);
  }
}

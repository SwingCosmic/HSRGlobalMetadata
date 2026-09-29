namespace HSRGlobalMetadata.Configuration;

public sealed record EncryptedField(
    int Offset,
    MetadataOperation Operation,
    long Operand
) {
    public static EncryptedField Sub(int offset, long operand) => new(offset, MetadataOperation.SUB, operand);
    public static EncryptedField Xor(int offset, long operand) => new(offset, MetadataOperation.XOR, operand);

    public long Apply(long raw) => Operation switch {
        MetadataOperation.XOR => raw ^ Operand,
        MetadataOperation.SUB => raw - Operand,
        MetadataOperation.ADD => raw + Operand,
        _ => raw
    };

    public int DecodeInt32(byte[] bytes, int baseOffset = 0) {
        int raw = BitConverter.ToInt32(bytes, baseOffset + Offset);
        return (int)Apply(raw);
    }
}

public sealed record RegistrationLayout(
    string Name,
    int CodeRegistrationSize,
    int MetadataRegistrationSize,
    int MetadataTablesSize,
    int MethodPointerOffset,
    EncryptedField TypeInfoCount,
    int GenericInstsOffset,
    int TypesRvaOffset,
    int ArrayOffset,
    int StringLiteralRvaOffset,
    EncryptedField StringLiteralCount
) {
    public const int SharedStringLiteralCountXor = 0xBD08DC8;

    public static RegistrationLayout OsProdWin440 { get; } = new(
        Name: "OsProdWin440",
        CodeRegistrationSize: 0x100,
        MetadataRegistrationSize: 0x100,
        MetadataTablesSize: 0x68,
        MethodPointerOffset: 0x88,
        TypeInfoCount: EncryptedField.Sub(0x80, 458010256),
        GenericInstsOffset: 0x20,
        TypesRvaOffset: 0x68,
        ArrayOffset: 0x90,
        StringLiteralRvaOffset: 0x28,
        StringLiteralCount: EncryptedField.Xor(0x40, SharedStringLiteralCountXor)
    );

    public static RegistrationLayout OsProdWin450 { get; } = new(
        Name: "OsProdWin450",
        CodeRegistrationSize: 0x100,
        MetadataRegistrationSize: 0x100,
        MetadataTablesSize: 0x68,
        MethodPointerOffset: 0x40,
        TypeInfoCount: EncryptedField.Sub(0x48, 1455078204),
        GenericInstsOffset: 0x38,
        TypesRvaOffset: 0x80,
        ArrayOffset: 0x70,
        StringLiteralRvaOffset: 0x10,
        StringLiteralCount: EncryptedField.Xor(0x2C, SharedStringLiteralCountXor)
    );
}

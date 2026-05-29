namespace HSRGlobalMetadata;

public enum MetadataOperation { XOR, SUB, ADD }

[AttributeUsage(AttributeTargets.Property)]
public class MetadataTagAttribute : Attribute {
    public int Offset { get; }
    public MetadataOperation Operation { get; }
    public long Operand { get; }

    public MetadataTagAttribute(int offset, MetadataOperation operation, long operand) {
        Offset = offset;
        Operation = operation;
        Operand = operand;
    }

    public MetadataTagAttribute(int offset) {
        Offset = offset;
        Operation = MetadataOperation.ADD;
        Operand = 0;
    }
}
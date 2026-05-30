using System.Text.Json;

using HSRGlobalMetadata.Structs;
using HSRGlobalMetadata.Structs.Definitions;

public static class StringLiteralWriter {
  public static void Write(string inputDir) {
    string outputDir = Path.Combine(inputDir, "dump");
    Directory.CreateDirectory(outputDir);
    string outputPath = Path.Combine(outputDir, "stringliterals.json");

    List<StringLiteralEntry> stringLiterals = new();

    for (int i = 0; i < MetadataTables.Instance.StringLiteralCount; i++) {
      var literal = new Il2CppStringLiteral(i);
      stringLiterals.Add(new StringLiteralEntry{
        Address = $"0x{(ulong)MetadataTables.Instance.StringLiteralRva + 8 * (ulong)i:X}",
        Value = literal.Value
      });
    }
    File.WriteAllText(outputPath, JsonSerializer.Serialize(stringLiterals, new JsonSerializerOptions { WriteIndented = true }));
  }
}

public struct StringLiteralEntry {
  public string Address { get; init; }
  public string Value { get; init; }
}

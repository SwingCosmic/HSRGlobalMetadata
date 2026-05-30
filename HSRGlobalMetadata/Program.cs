using HSRGlobalMetadata.Output;
using HSRGlobalMetadata.Structs;

namespace HSRGlobalMetadata;

using Utils;

static class Program {
    static void Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var folderPath = args.Length > 0 && !string.IsNullOrEmpty(args[0])
            ? args[0]
            : Prompt();

        folderPath = folderPath.Trim('"');

        if (!Directory.Exists(folderPath)) {
            Console.WriteLine("Game directory does not exist.");
            return;
        }

        var gameAssemblyPath = Directory.GetFiles(folderPath, "GameAssembly.dll").FirstOrDefault();
        if (gameAssemblyPath == null) {
            Console.WriteLine("GameAssembly.dll not found.");
            return;
        }

        PEHelper.ReadPEHeader(gameAssemblyPath);

        string metadataPath = Path.Combine(
            folderPath,
            "StarRail_Data",
            "il2cpp_data",
            "Metadata",
            "global-metadata.dat"
        );
        
        if(!Path.Exists(metadataPath)) {
          Console.WriteLine("global-metadata.dat not found.");
          return;
        }

        string startupMetadataPath = Path.Combine(
            folderPath,
            "StarRail_Data",
            "il2cpp_data",
            "Metadata",
            "startup-metadata.dat"
        );

        if (!Path.Exists(startupMetadataPath)) {
          Console.WriteLine("startup-metadata.dat not found.");
          return;
        }           

        Console.WriteLine("Initializing...");
        MetadataContext.Initialize(metadataPath, startupMetadataPath, gameAssemblyPath);
        MetadataHeader.Initialize(gameAssemblyPath);
        MetadataRegistration.Initialize(gameAssemblyPath);
        CodeRegistration.Initialize(gameAssemblyPath);
        MetadataTables.Initialize(gameAssemblyPath);
        Console.WriteLine("Intiializing cache...");
        MetadataCache.Initialize();
        Console.WriteLine("Initialization complete.");

        Console.WriteLine("Writing dump.cs...");
        DumpWriter.Write(folderPath);

        Console.WriteLine("Writing stringliterals.json...");
        StringLiteralWriter.Write(folderPath);
        
        Console.WriteLine("Finished.");
    }

    static string Prompt() {
        Console.Write("Enter game folder: ");
        return Console.ReadLine() ?? "";
    }
}

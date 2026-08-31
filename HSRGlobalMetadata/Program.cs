using HSRGlobalMetadata.Configuration;
using HSRGlobalMetadata.DummyDll.Generation;
using HSRGlobalMetadata.Output;
using HSRGlobalMetadata.Structs;
using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata;

public static class Program {
    public static int Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        CommandLineOptions options;
        try {
            options = CommandLineParser.Parse(args);
        }
        catch (CommandLineException ex) {
            Console.Error.WriteLine($"Argument error: {ex.Message}");
            Console.Error.WriteLine(CommandLineParser.Usage);
            return 2;
        }

        if (options.ShowHelp) {
            Console.WriteLine(CommandLineParser.Usage);
            return 0;
        }

        string folderPath = options.GameDirectory ?? Prompt();
        folderPath = Path.GetFullPath(folderPath.Trim().Trim('"'));
        if (!Directory.Exists(folderPath))
            return Fail($"Game directory does not exist: {folderPath}");

        VersionProfile profile;
        try {
            profile = options.ResolveProfile();
            RuntimeConfiguration.Initialize(profile);
        }
        catch (Exception ex) when (ex is CommandLineException or ArgumentException) {
            return Fail(ex.Message, 2);
        }

        string gameAssemblyPath = Path.Combine(folderPath, "GameAssembly.dll");
        string metadataPath = Path.Combine(folderPath, "StarRail_Data", "il2cpp_data", "Metadata", "global-metadata.dat");
        string startupMetadataPath = Path.Combine(folderPath, "StarRail_Data", "il2cpp_data", "Metadata", "startup-metadata.dat");

        if (!File.Exists(gameAssemblyPath))
            return Fail($"GameAssembly.dll not found: {gameAssemblyPath}");
        if (!File.Exists(metadataPath))
            return Fail($"global-metadata.dat not found: {metadataPath}");
        if (!File.Exists(startupMetadataPath))
            return Fail($"startup-metadata.dat not found: {startupMetadataPath}");

        string outputRoot = options.OutputDirectory == null
            ? Path.Combine(folderPath, "dump")
            : Path.GetFullPath(options.OutputDirectory);

        PrintEffectiveConfiguration(options, profile, folderPath, outputRoot);

        try {
            PEHelper.ReadPEHeader(gameAssemblyPath);

            Console.WriteLine("Initializing metadata...");
            MetadataContext.Initialize(metadataPath, startupMetadataPath, gameAssemblyPath);
            MetadataHeader.Initialize(gameAssemblyPath);
            MetadataRegistration.Initialize(gameAssemblyPath);
            CodeRegistration.Initialize(gameAssemblyPath);
            MetadataTables.Initialize(gameAssemblyPath);
            Console.WriteLine("Initializing cache...");
            MetadataCache.Initialize();
            if (options.GenerateDummyDll)
                PrintIl2CppTypeBitDistribution();
            Console.WriteLine("Initialization complete.");

            if (options.GenerateDump) {
                Console.WriteLine("Writing dump.cs...");
                DumpWriter.WriteToDirectory(outputRoot);
            }

            if (options.GenerateStringLiterals) {
                Console.WriteLine("Writing stringliterals.json...");
                StringLiteralWriter.WriteToDirectory(outputRoot);
            }

            if (options.GenerateDummyDll) {
                Console.WriteLine("Writing metadata DummyDll assemblies...");
                DummyDllExportResult result = DummyAssemblyExporter.ExportAssemblies(outputRoot);
                Console.WriteLine(
                    $"Wrote {result.AssemblyPaths.Count} DummyDll assemblies with " +
                    $"{result.Report.TypeCount} types, {result.Report.FieldCount} fields and " +
                    $"{result.Report.PropertyCount} properties, {result.Report.MethodCount} methods and " +
                    $"{result.Report.EventCount} events to " +
                    $"{Path.Combine(outputRoot, DummyAssemblyExporter.DirectoryName)}."
                );
                Console.WriteLine(
                    $"Serializable public instance fields: {result.Report.SerializableFieldCount}; " +
                    $"serializable-field placeholders: {result.Report.SerializableFieldPlaceholderCount}; " +
                    $"all type placeholders: {result.Report.PlaceholderTypeCount}; " +
                    $"prepared public members: {result.Report.PreparedPublicMemberCount}; " +
                    $"public-member placeholders: {result.Report.PublicMemberPlaceholderCount}; " +
                    $"addressed methods: {result.Report.AddressedMethodCount}; " +
                    $"diagnostics: {result.Report.Diagnostics.Count}; report: {result.ReportPath}"
                );
            }

            Console.WriteLine("Finished.");
            return 0;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed: {ex.Message}");
            if (options.Strict)
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void PrintEffectiveConfiguration(
        CommandLineOptions options,
        VersionProfile profile,
        string gameDirectory,
        string outputRoot
    ) {
        Console.WriteLine("Effective configuration:");
        Console.WriteLine($"  Game directory: {gameDirectory}");
        Console.WriteLine($"  Output root: {outputRoot}");
        Console.WriteLine($"  Version: {profile.Name}");
        Console.WriteLine($"  Metadata magic: {profile.MetadataMagicText}");
        Console.WriteLine($"  ImageBase: {profile.ImageBaseText}");
        Console.WriteLine($"  Type/Image/Method strides: {profile.Layout.TypeDefinitionSize}/{profile.Layout.ImageDefinitionSize}/{profile.Layout.MethodDefinitionSize}");
        Console.WriteLine($"  Outputs: dump.cs={options.GenerateDump}, stringliterals.json={options.GenerateStringLiterals}, DummyDll={options.GenerateDummyDll}");
        Console.WriteLine($"  Strict validation: {options.Strict}");
    }

    private static int Fail(string message, int exitCode = 1) {
        Console.Error.WriteLine(message);
        return exitCode;
    }

    private static void PrintIl2CppTypeBitDistribution() {
        int bit5 = MetadataCache.Types.Count(type => (type.PackedFlags & 0x20) != 0);
        int bit6 = MetadataCache.Types.Count(type => (type.PackedFlags & 0x40) != 0);
        int bit7 = MetadataCache.Types.Count(type => (type.PackedFlags & 0x80) != 0);
        Console.WriteLine(
            $"Il2CppType high-bit distribution: bit5={bit5}, bit6={bit6}, bit7={bit7}; " +
            $"configured byref={MetadataCache.Types.Count(type => type.IsByReference)}, " +
            $"pinned={MetadataCache.Types.Count(type => type.IsPinned)}."
        );
    }

    private static string Prompt() {
        Console.Write("Enter game folder: ");
        return Console.ReadLine() ?? "";
    }
}

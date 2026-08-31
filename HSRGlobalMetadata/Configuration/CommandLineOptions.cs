using System.Globalization;

namespace HSRGlobalMetadata.Configuration;

public sealed record CommandLineOptions(
    string? GameDirectory,
    string? OutputDirectory,
    string Version,
    uint? MetadataMagicOverride,
    ulong? ImageBaseOverride,
    bool GenerateDummyDll,
    bool GenerateDump,
    bool GenerateStringLiterals,
    bool Strict,
    bool ShowHelp
) {
    public VersionProfile ResolveProfile() {
        if (!VersionProfiles.TryGet(Version, out var profile)) {
            var supported = string.Join(", ", VersionProfiles.Names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            throw new CommandLineException($"Unknown version '{Version}'. Supported versions: {supported}.");
        }

        return profile with {
            MetadataMagic = MetadataMagicOverride ?? profile.MetadataMagic,
            ImageBase = ImageBaseOverride ?? profile.ImageBase
        };
    }
}

public sealed class CommandLineException(string message) : Exception(message);

public static class CommandLineParser {
    public static CommandLineOptions Parse(IReadOnlyList<string> args) {
        string? gameDirectory = null;
        string? outputDirectory = null;
        string version = VersionProfiles.DefaultName;
        uint? metadataMagic = null;
        ulong? imageBase = null;
        bool generateDummyDll = false;
        bool generateDump = true;
        bool generateStringLiterals = true;
        bool strict = false;
        bool showHelp = false;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Count; i++) {
            string arg = args[i];
            if (!arg.StartsWith("-", StringComparison.Ordinal)) {
                if (gameDirectory != null)
                    throw new CommandLineException($"Unexpected positional argument '{arg}'. Only one game directory is allowed.");
                gameDirectory = TrimQuotes(arg);
                continue;
            }

            string option = arg.ToLowerInvariant();
            switch (option) {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--dummy-dll":
                    EnsureUnique(seen, option);
                    generateDummyDll = true;
                    break;
                case "--no-dump":
                    EnsureUnique(seen, option);
                    generateDump = false;
                    break;
                case "--no-string-literals":
                    EnsureUnique(seen, option);
                    generateStringLiterals = false;
                    break;
                case "--strict":
                    EnsureUnique(seen, option);
                    strict = true;
                    break;
                case "--output":
                    EnsureUnique(seen, option);
                    outputDirectory = TrimQuotes(ReadValue(args, ref i, option));
                    break;
                case "--version":
                    EnsureUnique(seen, option);
                    version = ReadValue(args, ref i, option);
                    break;
                case "--metadata-magic":
                    EnsureUnique(seen, option);
                    metadataMagic = ParseUInt32(ReadValue(args, ref i, option), option);
                    break;
                case "--image-base":
                    EnsureUnique(seen, option);
                    imageBase = ParseUInt64(ReadValue(args, ref i, option), option);
                    break;
                default:
                    throw new CommandLineException($"Unknown option '{arg}'.");
            }
        }

        if (!generateDump && !generateStringLiterals && !generateDummyDll && !showHelp)
            throw new CommandLineException("No output is enabled. Remove at least one --no-* option or add --dummy-dll.");

        return new CommandLineOptions(
            gameDirectory,
            outputDirectory,
            version,
            metadataMagic,
            imageBase,
            generateDummyDll,
            generateDump,
            generateStringLiterals,
            strict,
            showHelp
        );
    }

    public static string Usage => """
        Usage:
          HSRGlobalMetadata <game-directory> [options]

        Options:
          --dummy-dll                 Generate metadata DummyDll assemblies.
          --output <directory>        Output directory. Default: <game-directory>/dump.
          --version <name>            Metadata profile. Default: OSPRODWin4.5.0.
          --metadata-magic <number>   Override metadata magic (decimal or 0x hexadecimal).
          --image-base <number>       Override PE image base (decimal or 0x hexadecimal).
          --no-dump                   Do not generate dump.cs.
          --no-string-literals        Do not generate stringliterals.json.
          --strict                    Treat recoverable validation diagnostics as errors.
          -h, --help                  Show this help.
        """;

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option) {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal))
            throw new CommandLineException($"Option '{option}' requires a value.");
        return args[++index];
    }

    private static void EnsureUnique(HashSet<string> seen, string option) {
        if (!seen.Add(option))
            throw new CommandLineException($"Option '{option}' was specified more than once.");
    }

    private static string TrimQuotes(string value) => value.Trim().Trim('"');

    private static uint ParseUInt32(string value, string option) {
        ulong parsed = ParseUInt64(value, option);
        if (parsed > uint.MaxValue)
            throw new CommandLineException($"Value '{value}' for '{option}' exceeds UInt32.MaxValue.");
        return (uint)parsed;
    }

    private static ulong ParseUInt64(string value, string option) {
        string digits = value;
        NumberStyles style = NumberStyles.Integer;
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            digits = value[2..];
            style = NumberStyles.AllowHexSpecifier;
        }

        if (digits.Length == 0 || !ulong.TryParse(digits, style, CultureInfo.InvariantCulture, out ulong parsed))
            throw new CommandLineException($"Value '{value}' for '{option}' is not a valid decimal or hexadecimal number.");
        return parsed;
    }
}

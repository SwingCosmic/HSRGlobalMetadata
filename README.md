# HSRGlobalMetadata

A proof-of-concept static analysis tool to extract metadata information from `Honkai: Star Rail`.

## About
This tool generates a `dump.cs` and `stringliterals.json` file extracted from the metadata of the game, and from the `GameAssembly.dll` binary. It works completely statically, which means launching the game process is not required.

The tool has been tested to work with the `OSPRODWin4.5.0` version of the game.

## Important
This tool is a proof-of-concept. Some features may be missing, it may be unstable, or break with game updates. Older versions are not supported, and newer versions can break the tool.

## Usage
Builds of this project will not be provided.

.NET 10.0 is required.

To run it, simply do `dotnet run <path_to_game_folder>`.

For faster runtime: `dotnet run -c Release <path_to_game_folder>`

To generate metadata DummyDll assemblies in addition to the existing outputs:

```text
dotnet run -c Release -- <path_to_game_folder> --dummy-dll
```

The default output directory is `<path_to_game_folder>/dump`. DummyDll assemblies are written to its `DummyDll` child directory.
The generator currently restores type/nesting/inheritance/interface relationships, type generic parameters,
fields, literal constants, field offsets, arrays, closed generic field graphs, and properties with their
minimal accessor bodies. A `generation-report.json` file is emitted beside the assemblies with coverage and
fallback diagnostics. Delegate-specific method skeletons and the remaining ordinary methods/events are planned
for phase three.

Available options:

```text
--dummy-dll                 Generate DummyDll assemblies.
--output <directory>        Override the output directory.
--version <name>            Select a metadata profile (default: OSPRODWin4.5.0).
--metadata-magic <number>   Override metadata magic; decimal and 0x hex are accepted.
--image-base <number>       Override ImageBase; decimal and 0x hex are accepted.
--no-dump                   Skip dump.cs generation.
--no-string-literals        Skip stringliterals.json generation.
--strict                    Include full diagnostics when generation fails.
```

For example, to generate only DummyDll files:

```text
dotnet run -c Release -- <path_to_game_folder> --dummy-dll --no-dump --no-string-literals
```

## Disclaimer
This tool is only for educational purposes. I do not take any responsibility for the usage of this tool.

## License
This project is licensed under the GPL-3.0 license. See [LICENSE](LICENSE) for more details.

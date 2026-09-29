# HSRGlobalMetadata

A proof-of-concept static analysis tool to extract metadata information from `Honkai: Star Rail`.

## About
This tool generates a `dump.cs` and `stringliterals.json` file extracted from the metadata of the game, and from the `GameAssembly.dll` binary. It works completely statically, which means launching the game process is not required.

The tool has been tested with the `OSPRODWin4.6.0` version of the game. Built-in profiles are selected with `--version`:

| Profile | Il2CppType record | Registration slots |
| --- | --- | --- |
| `OSPRODWin4.3.0` | 8-byte type index (`IndexBased8`) | `OsProdWin440` |
| `OSPRODWin4.4.0` | 16-byte VA (`VaBased16`) | `OsProdWin440` |
| `OSPRODWin4.5.0` | 16-byte VA (`VaBased16`) | `OsProdWin450` |
| `OSPRODWin4.6.0` (default) | 8-byte type index (`IndexBased8`) | `OsProdWin450` |

`OSPRODWin4.5.0` and `OSPRODWin4.6.0` are the verified combinations. `OSPRODWin4.3.0` and `OSPRODWin4.4.0` reuse the original author's published offsets as the same two modes. A later game version that only switches one of those axes is a new dictionary entry in `VersionProfiles`, plus a new named mode if an offset table actually changed.

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

### DummyDll support

DummyDll generation restores the metadata structure needed by common .NET inspection tools:

- types, nesting, inheritance, interfaces, generic parameters, and generic constraints;
- structured generic, array, pointer, and by-reference type signatures;
- public and non-public fields, properties, methods, parameters, and events;
- literal values, field offsets, property/event accessors, and delegate signatures; and
- method VA, RVA, and PE file offsets when a valid method pointer is available.

The generated assemblies can be loaded with Mono.Cecil and browsed with tools such as dnSpy or ILSpy. DummyDll
support is currently verified for the built-in `OSPRODWin4.6.0` and `OSPRODWin4.5.0` profiles. Other game versions may require a new
named type-record or registration mode, then a new `VersionProfiles` entry that composes them.

DummyDll files contain metadata stubs, not reconstructed game code. Ordinary managed methods return default
values, and metadata that is not available from the current parser is not fabricated.

### Why use DummyDll instead of a large `dump.cs`?

A `dump.cs` is useful as a single searchable text file, but it becomes cumbersome when it grows beyond 100 MB.
DummyDll output provides several practical advantages:

- open only the assembly you need instead of scanning or parsing one very large text file;
- navigate types and cross-assembly references directly in dnSpy or ILSpy;
- consume a structured type/member graph through Mono.Cecil without writing a custom C# text parser;
- preserve generics, nesting, arrays, pointers, and ref/out signatures as metadata nodes; and
- read field offsets and method addresses directly from custom attributes.

This makes DummyDll output better suited to IDE-style browsing, reflection-like tooling, and automated metadata
processing, while `dump.cs` remains available for plain-text searches and quick manual inspection.

Available options:

```text
--dummy-dll                 Generate DummyDll assemblies.
--output <directory>        Override the output directory.
--version <name>            Select a metadata profile (default: OSPRODWin4.6.0).
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

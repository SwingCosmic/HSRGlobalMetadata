# PR Draft: Add Metadata DummyDll Generation

## Suggested title

`feat: add metadata DummyDll generation support`

## Summary

This PR adds opt-in metadata DummyDll generation while preserving the existing HSR metadata parsing pipeline and default outputs. With `--dummy-dll`, the tool now converts the existing parsed HSR metadata and game assembly inputs into managed assemblies that can be consumed by Mono.Cecil, dnSpy, ILSpy, and other metadata-based tooling.

The implementation is ready for its intended use:

- **100% type coverage on the validated sample**, with no unresolved type placeholders.
- **Correct VA, RVA, and PE file offsets** for every method with a valid method pointer.
- **Successful real-world data deserialization** using the generated types with data from [DimbreathBot/TurnBasedGameData](https://github.com/DimbreathBot/TurnBasedGameData).
- **143/143 generated assemblies reload successfully** through Mono.Cecil.
- **21/21 automated tests pass**, and the existing `dump.cs` and `stringliterals.json` outputs remain unchanged.

These DLLs are metadata stubs, not reconstructed game code. They preserve the type and member structure required for inspection, navigation, and data processing. Ordinary managed methods contain only minimal valid IL and do not represent the original game implementation.

## Validation highlights

| Validation | Result |
|---|---:|
| Generated assemblies reloaded by Mono.Cecil | 143/143 |
| Types | 80,960 |
| Metadata fields | 555,717 |
| Properties | 91,745 |
| Methods | 733,062 |
| Events | 753 |
| Generic constraints | 2,437 |
| Methods with validated address metadata | 700,927 |
| Parameters emitted as `ByReferenceType` | 40,502 |
| Serializable public instance fields | 107,771 |
| Type placeholders | 0 |
| Public-member placeholders | 0 |
| Warning/error diagnostics | 0 |
| Automated tests | 21/21 passed |

The public-member validation gate covers **908,806 members**:

- 401,175 fields;
- 80,112 properties;
- 426,840 methods;
- 679 events.

An independent pass over the written DLLs reproduced the same public-member, address, generic-constraint, and byref-parameter counts reported during generation.

## What changed

### Command-line and versioned configuration

- Added `--dummy-dll` to enable DummyDll generation explicitly.
- Added `--output`, `--version`, `--metadata-magic`, `--image-base`, `--no-dump`, `--no-string-literals`, and `--strict` options.
- Preserved the original positional game-directory argument and the default generation of `dump.cs` and `stringliterals.json`.
- Centralized the metadata magic, ImageBase, structure sizes, and `Il2CppType` bit layout in a version profile.
- Added the validated `OSPRODWin4.5.0` profile as the default.
- Unknown versions without sufficient overrides fail with a clear diagnostic instead of silently producing potentially invalid assemblies.

### HSR metadata adapter

- Added a read-only adapter that converts the existing HSR metadata, startup metadata, and PE data into a normalized model for the generator.
- Kept HSR-specific decryption, index conversion, virtual-address handling, and bounds checking inside the adapter.
- Extended the existing metadata structures with the header, table, generic-constraint, method-pointer, and `Il2CppType.bits` data needed by DummyDll generation.
- Added explicit range and index validation with image, type, and member context in diagnostics.
- Missing metadata is omitted or reported rather than fabricated.

### Structured Cecil type graph

The generator creates Cecil `TypeReference` objects structurally instead of rebuilding types from display-name strings. The validated graph covers:

- primitive, CLASS, and VALUETYPE references;
- open VAR/MVAR parameters and closed `GENERICINST` references;
- ARRAY, SZARRAY, PTR, and BYREF references;
- type and method generic parameters and constraints;
- nested types, inheritance, interfaces, and cross-assembly references.

The `num_mods`, `byref`, and `pinned` values are decoded through the active version profile. The validated HSR layout uses a 6-bit `num_mods`, bit 6 for `byref`, and bit 7 for `pinned`.

### Multi-pass assembly generation

The generator creates stable Cecil nodes before resolving relationships that depend on them:

```text
CLI and version profile
    -> existing HSR metadata initialization and cache
    -> normalized HSR metadata adapter
    -> assembly and type skeletons
    -> nesting, generics, inheritance, and interfaces
    -> field and method skeletons
    -> method signatures and generic constraints
    -> properties, accessors, and events
    -> field offsets and method address attributes
    -> staged write and Mono.Cecil reload validation
    -> directory replacement and generation report
```

This includes:

- one output assembly for each metadata image;
- type definitions, nesting, inheritance, interfaces, and generic ownership;
- field flags, literal values, and `FieldOffsetAttribute` values;
- complete method and parameter signatures;
- property getter/setter and event add/remove/raise associations;
- reuse of existing backing fields for ordinary read/write properties, with separately reported synthetic backing fields when needed;
- minimal valid IL for ordinary managed methods according to their return type;
- correct no-body behavior for abstract, interface, P/Invoke, and delegate runtime methods.

### Method addresses

Methods with valid pointers receive `AddressAttribute` values calculated as follows:

```text
VA     = MethodPointer
RVA    = MethodPointer - EffectiveImageBase
Offset = PEHelper.RvaToOffset(RVA)
```

The ImageBase comes from the active version profile. A zero pointer, an RVA outside the mapped PE sections, or an unmappable address produces a contextual diagnostic instead of fabricated address metadata.

### Safe output and reporting

- Assemblies are written to a staging directory first.
- Every written DLL is immediately reopened with Mono.Cecil to validate its PE/CLI structure and module name.
- The final `DummyDll/` directory is replaced only after all assemblies pass validation; failed generation cleans up temporary output and restores the previous directory when possible.
- Generation produces `DummyDll/generation-report.json` with assembly, type, member, serializable-field, public-member, placeholder, address, generic-constraint, synthetic-field, and diagnostic counts.

Both `DummyDll/` and `DummyDll/generation-report.json` are runtime outputs produced by this implementation; the PR does not depend on local sample files or uncommitted documentation.

## Detailed verification

### Automated and regression tests

- **21/21 unit and regression tests pass.**
- Tests cover command-line parsing, version configuration, `Il2CppType` bit decoding, the embedded template, structured type references, nested types, generics, automatic properties, methods, events, byref parameters, address attributes, fallback behavior, and diagnostics.
- Running without `--dummy-dll` preserves the existing behavior.
- In real-sample regression testing, the old and new `dump.cs` outputs were both 136,224,400 bytes with identical SHA-256 hashes.
- The old and new `stringliterals.json` outputs were both 11,735,776 bytes with identical SHA-256 hashes.
- An invalid metadata magic value fails before metadata parsing and returns a non-zero exit code.

### Real-sample structural validation

- All 143 assemblies were written and reopened successfully through Mono.Cecil.
- Type coverage reached 100%, including generics, arrays, pointers, byref, VAR/MVAR, nested types, and cross-assembly references.
- All 107,771 serializable public instance fields use structured types; none required a placeholder.
- The 22,506 diagnostics are informational records for synthetic backing fields. They are not warnings or errors and are not counted as metadata-field coverage.
- Non-public fields, properties, methods, and events are also emitted. All 306,222 non-public methods have a shape valid for their metadata semantics, with zero unexpected missing method bodies.
- There are no properties or events with missing accessors, no accessor-owner mismatches, and no `__invalid_*` fallback members.

### Address and deserialization validation

- Every method with a valid method pointer has a confirmed VA, RVA, and PE file offset.
- Reading the generated `AddressAttribute` values back from the DLLs produced the expected address values.
- Representative configuration types, field types, offsets, enum literals, properties, and generic relationships were inspected manually.
- The generated type graph was used successfully to deserialize real Honkai: Star Rail release data from [DimbreathBot/TurnBasedGameData](https://github.com/DimbreathBot/TurnBasedGameData). This validates the primary practical use case beyond structural inspection alone.

## Validation boundary and intentional scope

A memory-bounded full diff against the approximately 136 MB `dump.cs` has not yet been run. That comparison would provide an additional reproducible, text-based coverage gate, but it is not required to establish the current results:

- type coverage is already 100%, with zero type placeholders;
- all public members are prepared, with zero public-member placeholders;
- all generated assemblies reload successfully;
- valid method addresses are correct; and
- real data deserialization has been validated successfully.

The current implementation therefore meets the requirements for submission. The large `dump.cs` comparison can be added later as an independent validation tool.

The following items are intentionally out of scope because the current parser does not provide a reliable source for them: parameter default values, custom-attribute blobs, original tokens, slots, property attributes, and method implementation flags. The PR also does not add registration scanning, IDA/Ghidra scripts, or comprehensive AssetStudio compatibility. Missing information is omitted or diagnosed, never presented as original metadata.

## Usage

Generate DummyDll assemblies in addition to the existing outputs:

```text
dotnet run -c Release -- <path_to_game_folder> --dummy-dll
```

Generate only DummyDll assemblies:

```text
dotnet run -c Release -- <path_to_game_folder> --dummy-dll --no-dump --no-string-literals
```

The default output is `<path_to_game_folder>/dump/DummyDll`. The built-in configuration is currently validated for `OSPRODWin4.5.0`; other versions may require a new metadata layout or explicit configuration overrides.

## Submission checklist

- [x] The PR is opt-in and preserves the existing default behavior.
- [x] 21/21 automated tests pass.
- [x] 143/143 output assemblies reload successfully through Mono.Cecil.
- [x] Type coverage is 100%, with zero type placeholders.
- [x] The public-member validation gate passes with zero public-member placeholders.
- [x] Valid method VA, RVA, and PE file offsets are correct.
- [x] Generated types successfully deserialize real data from [DimbreathBot/TurnBasedGameData](https://github.com/DimbreathBot/TurnBasedGameData).
- [ ] Run the memory-bounded full diff against the 136 MB `dump.cs` as a follow-up, non-blocking validation step.

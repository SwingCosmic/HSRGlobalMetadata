# HSRGlobalMetadata

A proof-of-concept static analysis tool to extract metadata information from `Honkai: Star Rail`.

## About
This tool generates a `dump.cs` file extracted from the metadata of the game, and from the `GameAssembly.dll` binary. It works completely statically, which means launching the game process is not required.

The tool has been tested to work with the `OSPRODWin4.3.0` version of the game.

## Important
This tool is a proof-of-concept. Some features may be missing, it may be unstable, or break with game updates. Older versions are not supported, and newer versions can break the tool.

## Usage
Builds of this project will not be provided.

NET 10.0 is required.

To run it, simply do `dotnet run <path_to_game_folder>`.

For faster runtime: `dotnet run -c Release <path_to_game_folder>`

## Disclaimer
This tool is only for educational purposes. I do not take any responsibility for the usage of this tool.

## License
This project is licensed under the GPL-3.0 license. See [LICENSE](LICENSE) for more details.

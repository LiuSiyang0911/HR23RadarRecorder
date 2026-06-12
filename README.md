# HR2.3 Radar Recorder

Independent .NET 8 Windows Forms project skeleton for the HR2.3 radar recorder. This stage contains environment setup, configuration loading, and a minimal startup window only. It does not implement UDP acquisition, TCP control, radar processing, or file recording.

## Requirements

- Windows 10 or Windows 11, x64
- .NET 8 SDK
- Visual Studio Code (recommended)
- C# Dev Kit or the C# extension for VS Code (recommended)

Visual Studio IDE is not required. MATLAB Runtime, `HR.dll`, `MWArray.dll`, and output from the legacy HR2.3 project are not used.

## First-Time Setup

1. Install the .NET 8 SDK.
2. Install Visual Studio Code.
3. Install C# Dev Kit or the C# extension.
4. Open this `HR23RadarRecorder` directory in VS Code.
5. Open a terminal in the project root.
6. Run `dotnet restore`.
7. Run `dotnet build`.
8. Run `dotnet run --project src/HR23RadarRecorder.App`.

Verify the SDK before building:

```powershell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

The repository `global.json` keeps SDK selection on .NET 8. It requests `8.0.422` with `latestFeature` roll-forward because that is the stable .NET 8 SDK used for project verification. This prevents another major .NET version from silently taking its place.

## CLI Commands

Run these commands from the directory containing this README:

```powershell
dotnet restore
dotnet build
dotnet run --project src/HR23RadarRecorder.App
dotnet publish src/HR23RadarRecorder.App -c Release -r win-x64 --self-contained false
```

Create a self-contained Windows x64 package when the destination computer may not have the .NET 8 Desktop Runtime:

```powershell
dotnet publish src/HR23RadarRecorder.App -c Release -r win-x64 --self-contained true
```

Framework-dependent output is written below `src/HR23RadarRecorder.App/bin/Release/net8.0-windows/win-x64/publish/`. Build and publish directories are intentionally ignored by Git.

## VS Code

- Use **Terminal > Run Task** to select `restore`, `build`, `run`, or `publish`.
- Use `Ctrl+Shift+B` for the default build task.
- Press `F5` to restore, build, and debug the application.

No Visual Studio project-property setup is required.

## Configuration

The application reads `src/HR23RadarRecorder.App/appsettings.json`. The file is copied beside the executable during build and publish.

```json
{
  "udp": {
    "localIp": "192.168.0.110",
    "localPort": 20202,
    "remoteIp": "192.168.0.255",
    "remotePort": 23480
  },
  "control": {
    "host": "127.0.0.1",
    "port": 7070
  },
  "recording": {
    "defaultOutputRoot": "records"
  }
}
```

`recording.defaultOutputRoot` is relative by default for portability. It may be changed to an absolute deployment path such as `D:/HR23RadarRecords` on a specific machine. If the configuration file is missing, empty, unreadable, or invalid JSON, the program uses built-in defaults and displays a notice.

The endpoint values are configuration placeholders only. The current skeleton does not open sockets or write recordings.

## Dependencies

There are no third-party NuGet packages. The project uses only the .NET SDK, Windows Forms, `System.IO`, and `System.Text.Json`.

Forbidden legacy dependencies are not referenced:

- MATLAB Runtime
- `HR.dll`
- `MWArray.dll`
- MathWorks assemblies
- legacy project `bin`, `obj`, or `.vs` output
- legacy project absolute paths

## Current Machine Check

Initial environment inspection on June 12, 2026 found system SDKs `5.0.409` and `6.0.101`; no system-wide .NET 8 SDK was installed. SDK `8.0.422` was then installed in a temporary user directory solely to verify this skeleton. A normal terminal still requires a stable .NET 8 SDK installation available through `PATH`. Visual Studio IDE is not needed for that installation.

See [docs/environment.md](docs/environment.md) for detailed setup, VS Code debugging, and troubleshooting.

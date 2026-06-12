# Development Environment

## Supported Systems

- Windows 10 x64
- Windows 11 x64

The application targets `net8.0-windows` and uses Windows Forms.

## Required Software

1. Install the .NET 8 SDK from <https://dotnet.microsoft.com/download/dotnet/8.0>.
2. Confirm installation with `dotnet --info` and `dotnet --list-sdks`.
3. Install Visual Studio Code from <https://code.visualstudio.com/>.
4. Install either C# Dev Kit or the C# extension in VS Code.

Visual Studio IDE, MATLAB Runtime, `HR.dll`, and `MWArray.dll` are not required.

## SDK Version Policy

`global.json` requests SDK `8.0.422` and uses `latestFeature` roll-forward. Version `8.0.422` is the stable SDK used to verify this skeleton. This keeps the project on the .NET 8 major/minor line while allowing a later supported .NET 8 feature-band SDK to be selected. Preview SDKs are not accepted.

Environment checked on June 12, 2026:

```text
OS: Windows 11 (version 10.0.22631), x64
Selected SDK: 6.0.101

Installed SDKs:
5.0.409 [C:\Program Files\dotnet\sdk]
6.0.101 [C:\Program Files\dotnet\sdk]

Installed runtimes:
Microsoft.AspNetCore.App 3.1.26
Microsoft.AspNetCore.App 5.0.17
Microsoft.AspNetCore.App 6.0.1
Microsoft.AspNetCore.App 6.0.6
Microsoft.NETCore.App 3.1.26
Microsoft.NETCore.App 5.0.17
Microsoft.NETCore.App 6.0.1
Microsoft.NETCore.App 6.0.6
Microsoft.NETCore.App 6.0.8
Microsoft.WindowsDesktop.App 3.1.26
Microsoft.WindowsDesktop.App 5.0.17
Microsoft.WindowsDesktop.App 6.0.1
Microsoft.WindowsDesktop.App 6.0.6
Microsoft.WindowsDesktop.App 6.0.8

.NET 8 SDK installed:
No
```

The initial system environment must install a .NET 8 SDK before a normal terminal can restore or build this project. A temporary user-local SDK `8.0.422` was used for skeleton verification and does not replace a regular SDK installation available through `PATH`. Installing Visual Studio IDE is not necessary.

## Skeleton Verification

The required commands were first run against the original machine environment and correctly failed during SDK resolution because no .NET 8 SDK was installed. The official .NET SDK `8.0.422` was then installed in a temporary user directory for verification only.

With SDK `8.0.422`, the following checks completed on June 12, 2026:

```text
dotnet restore: passed
dotnet build: passed (0 warnings, 0 errors)
dotnet run --project src/HR23RadarRecorder.App: started and remained active
framework-dependent win-x64 publish: passed
self-contained win-x64 publish: passed
JSON and project XML validation: passed
forbidden dependency and legacy path scan: passed
```

## Command-Line Workflow

Open PowerShell in the `HR23RadarRecorder` directory and run:

```powershell
dotnet restore
dotnet build
dotnet run --project src/HR23RadarRecorder.App
dotnet publish src/HR23RadarRecorder.App -c Release -r win-x64 --self-contained false
```

For a self-contained deployment:

```powershell
dotnet publish src/HR23RadarRecorder.App -c Release -r win-x64 --self-contained true
```

## VS Code Debugging

1. Open the `HR23RadarRecorder` directory in VS Code.
2. Wait for C# project discovery to finish.
3. Run **Terminal > Run Task > restore** once.
4. Press `Ctrl+Shift+B` to build.
5. Press `F5` and select **HR2.3 Radar Recorder** if prompted.

The checked-in `.vscode/tasks.json` defines restore, build, run, and publish tasks. The checked-in `.vscode/launch.json` starts the WinForms application under the .NET debugger.

## Troubleshooting

### `dotnet` command is not found

Install the x64 .NET 8 SDK, close and reopen the terminal, then run `dotnet --info`. Install the SDK, not only the runtime. Visual Studio IDE is not needed.

### VS Code does not recognize the C# project

Confirm that C# Dev Kit or the C# extension is enabled, open the project root rather than only a source file, and run `dotnet restore`. Restart VS Code after installing the SDK or extension.

### A port is already in use

The skeleton does not bind any ports yet. When networking is implemented, change the relevant port in `src/HR23RadarRecorder.App/appsettings.json` or stop the process currently using it. On Windows, `netstat -ano` can identify the owning process.

### The configured local IP cannot be bound

The skeleton does not bind the configured IP yet. For future networking builds, verify that the IP belongs to an active local adapter, run with appropriate Windows permissions, and update `udp.localIp` to the correct adapter address. Do not assume the sample address exists on every computer.

### The published executable does not run on another computer

A framework-dependent publish (`--self-contained false`) requires the matching .NET 8 Desktop Runtime on the target computer. Use `--self-contained true` to include the runtime, and publish for the correct architecture such as `win-x64`.

### The requested SDK cannot be resolved

Run `dotnet --list-sdks`. At least one stable `8.0.x` SDK must be installed. The `latestFeature` setting allows newer .NET 8 feature bands; it does not allow .NET 6 or .NET 9 to replace .NET 8.

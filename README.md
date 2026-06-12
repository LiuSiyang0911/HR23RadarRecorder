# HR2.3 Radar Recorder

Independent .NET 8 Windows Forms recorder for HR2.3 radar UDP data. Version 1.0 implements minimal synchronized raw recording: UDP payload capture, packet/event timestamps, metadata, a TCP JSON Lines control service, and manual GUI controls.

The application intentionally does not parse radar frames, run FFT/RDM/PPI algorithms, or depend on MATLAB, `HR.dll`, or `MWArray.dll`.

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
9. In the GUI, click **Start control server** if an external controller will connect.

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
- Use the `test` task to run the dependency-free integration tests.
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

The UDP socket is opened only after `start`. The TCP service is opened only after clicking **Start control server**. Change `udp.localIp` to an address assigned to the recorder computer before connecting real radar hardware.

## Recording Workflow

1. Start the GUI.
2. Start the TCP control service, or use the manual GUI controls.
3. Send/perform `prepare` with a session ID and output directory.
4. Send/perform `start`; UDP payloads are now appended to `raw.dat`.
5. Send/perform `stop`; wait for the `stopped` response before consuming files.

Start finalizes the `recording_started` timestamp, opens `raw.dat` and `packets.csv`, publishes the `recording` state, and only then starts the UDP receive loop. This ordering prevents the first packet from being discarded during startup. Stop publishes `stopping`, stops and joins the UDP receive loop, then flushes and closes every output file before returning.

Each capture directory contains:

```text
raw.dat
packets.csv
events.csv
metadata.json
```

With no radar traffic, start/stop still succeeds: `raw.dat` is empty, `packets.csv` contains its header, and events/metadata are finalized normally.

`packets.csv` is flushed every 100 packet rows to limit index loss after an abnormal exit without forcing a disk flush for every packet. A normal stop always performs a final flush for all remaining rows.

## Manual TCP Test

Start the control server in the GUI, then run this PowerShell snippet from another terminal:

```powershell
$client = [Net.Sockets.TcpClient]::new('127.0.0.1', 7070)
$stream = $client.GetStream()
$writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false), 1024, $true)
$reader = [IO.StreamReader]::new($stream, [Text.Encoding]::UTF8, $false, 1024, $true)
$writer.AutoFlush = $true

$commands = @(
  '{"cmd":"status"}',
  '{"cmd":"prepare","sessionId":"test_001","outputDir":"records/test_001/raw/hr23_radar"}',
  '{"cmd":"start"}',
  '{"cmd":"stop"}'
)

foreach ($command in $commands) {
  $writer.WriteLine($command)
  $reader.ReadLine()
}

$client.Dispose()
```

To inject ten mock UDP packets after the `start` command, run this in another PowerShell terminal. Use the configured local IP and port; the default example below targets loopback for local testing:

```powershell
$udp = [Net.Sockets.UdpClient]::new()
$target = [Net.IPEndPoint]::new([Net.IPAddress]::Loopback, 20202)

1..10 | ForEach-Object {
  $payload = [Text.Encoding]::UTF8.GetBytes("mock-hr23-packet-$($_)")
  [void]$udp.Send($payload, $payload.Length, $target)
}

$udp.Dispose()
```

For a fully local test, temporarily set `udp.localIp` to `127.0.0.1`, run `prepare` and `start`, send the mock packets, then run `stop`. Verify that `raw.dat` is non-empty, `packets.csv` has 11 lines (one header plus ten packets), and `metadata.json` reports `packetCount: 10`.

Output is below the `outputDir` supplied to `prepare`. See [docs/protocol.md](docs/protocol.md) and [docs/file_format.md](docs/file_format.md).

## Tests

The test runner uses only the .NET standard library:

```powershell
dotnet run --project tests/HR23RadarRecorder.Tests
```

It verifies invalid state responses, an empty recording, immediate first-packet capture, periodic packet CSV flushing, stop consistency under active UDP traffic, and the TCP JSON Lines control sequence.

## Dependencies

There are no third-party NuGet packages. The project uses only the .NET SDK, Windows Forms, sockets, `System.IO`, `System.Text.Json`, tasks, and `Stopwatch`.

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

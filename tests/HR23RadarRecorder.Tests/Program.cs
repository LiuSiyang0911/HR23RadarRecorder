using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HR23RadarRecorder.App.Control;
using HR23RadarRecorder.App.Core;
using HR23RadarRecorder.App.Radar;

return await TestRunner.RunAsync(
    ("invalid start is rejected", InvalidStartIsRejectedAsync),
    ("empty capture closes all files", EmptyCaptureClosesAllFilesAsync),
    ("udp packet is recorded verbatim", UdpPacketIsRecordedVerbatimAsync),
    ("tcp json lines controls capture", TcpJsonLinesControlsCaptureAsync));

static async Task InvalidStartIsRejectedAsync()
{
    await using CaptureRecorder recorder = CreateRecorder(GetFreeUdpPort());

    ControlResponse response = await recorder.StartAsync();

    Assert.False(response.Ok);
    Assert.Equal("invalid_state", response.Error);
    Assert.Equal("idle", response.State);
}

static async Task EmptyCaptureClosesAllFilesAsync()
{
    string outputDir = CreateTempDirectory();
    await using CaptureRecorder recorder = CreateRecorder(GetFreeUdpPort());

    Assert.True((await recorder.PrepareAsync(new PrepareCommand("empty", outputDir))).Ok);
    Assert.True((await recorder.StartAsync()).Ok);
    ControlResponse stop = await recorder.StopAsync();

    Assert.True(stop.Ok);
    Assert.Equal("stopped", stop.State);
    Assert.Equal(0L, stop.PacketCount);
    Assert.FileExists(Path.Combine(outputDir, "raw.dat"));
    Assert.FileExists(Path.Combine(outputDir, "packets.csv"));
    Assert.FileExists(Path.Combine(outputDir, "events.csv"));
    Assert.FileExists(Path.Combine(outputDir, "metadata.json"));
    Assert.Equal("Index,Utc,MonoNs,SessionElapsedNs,SenderIp,SenderPort,Length,FileOffset", File.ReadLines(Path.Combine(outputDir, "packets.csv")).First());

    string events = File.ReadAllText(Path.Combine(outputDir, "events.csv"));
    Assert.Contains("prepare_command_received", events);
    Assert.Contains("recording_started", events);
    Assert.Contains("raw_file_closed", events);
    Assert.Contains("stopped", events);
    Assert.True(events.IndexOf("prepare_command_received", StringComparison.Ordinal) < events.IndexOf("prepared", StringComparison.Ordinal));
    Assert.True(events.IndexOf("recording_started", StringComparison.Ordinal) < events.IndexOf("raw_file_closed", StringComparison.Ordinal));
    Assert.True(events.IndexOf("raw_file_closed", StringComparison.Ordinal) < events.IndexOf("stopped", StringComparison.Ordinal));

    using JsonDocument metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDir, "metadata.json")));
    Assert.Equal("empty", metadata.RootElement.GetProperty("session").GetProperty("sessionId").GetString());
    foreach (string section in new[] { "software", "session", "master", "control", "network", "timeBase", "files", "summary" })
    {
        Assert.True(metadata.RootElement.TryGetProperty(section, out _));
    }
}

static async Task UdpPacketIsRecordedVerbatimAsync()
{
    int port = GetFreeUdpPort();
    string outputDir = CreateTempDirectory();
    await using CaptureRecorder recorder = CreateRecorder(port);
    await recorder.PrepareAsync(new PrepareCommand("udp", outputDir));
    await recorder.StartAsync();

    byte[] payload = Encoding.ASCII.GetBytes("radar-packet");
    using UdpClient sender = new();
    await sender.SendAsync(payload, new IPEndPoint(IPAddress.Loopback, port));
    await WaitUntilAsync(() => recorder.GetSnapshot().PacketCount == 1, TimeSpan.FromSeconds(3));
    await recorder.StopAsync();

    Assert.SequenceEqual(payload, File.ReadAllBytes(Path.Combine(outputDir, "raw.dat")));
    string[] rows = File.ReadAllLines(Path.Combine(outputDir, "packets.csv"));
    Assert.Equal(2, rows.Length);
    Assert.Contains(",12,0", rows[1]);
    Assert.Equal(1L, recorder.GetSnapshot().PacketCount);
    Assert.Equal(12L, recorder.GetSnapshot().TotalBytes);
}

static async Task TcpJsonLinesControlsCaptureAsync()
{
    int udpPort = GetFreeUdpPort();
    int tcpPort = GetFreeTcpPort();
    string outputDir = CreateTempDirectory();
    await using CaptureRecorder recorder = CreateRecorder(udpPort);
    await using JsonLineControlServer server = new(IPAddress.Loopback, tcpPort, new ControlCommandHandler(recorder));
    await server.StartAsync();

    using TcpClient client = new();
    await client.ConnectAsync(IPAddress.Loopback, tcpPort);
    using StreamReader reader = new(client.GetStream(), Encoding.UTF8, leaveOpen: true);
    using StreamWriter writer = new(client.GetStream(), new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

    await writer.WriteLineAsync("{\"cmd\":\"status\"}");
    Assert.Contains("\"state\":\"idle\"", (await reader.ReadLineAsync())!);

    string prepare = JsonSerializer.Serialize(new { cmd = "prepare", sessionId = "tcp", outputDir });
    await writer.WriteLineAsync(prepare);
    Assert.Contains("\"state\":\"prepared\"", (await reader.ReadLineAsync())!);
    await writer.WriteLineAsync("{\"cmd\":\"start\"}");
    Assert.Contains("\"state\":\"recording\"", (await reader.ReadLineAsync())!);
    await writer.WriteLineAsync("{\"cmd\":\"stop\"}");
    Assert.Contains("\"state\":\"stopped\"", (await reader.ReadLineAsync())!);

    await server.StopAsync();
}

static CaptureRecorder CreateRecorder(int port) => new(
    new RadarNetworkConfig("127.0.0.1", port, "127.0.0.1", 23480),
    new ControlSettings { Host = "127.0.0.1", Port = 7070 },
    new TimeStampProvider());

static int GetFreeUdpPort()
{
    using UdpClient client = new(new IPEndPoint(IPAddress.Loopback, 0));
    return ((IPEndPoint)client.Client.LocalEndPoint!).Port;
}

static int GetFreeTcpPort()
{
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static string CreateTempDirectory()
{
    string path = Path.Combine(Path.GetTempPath(), "HR23RadarRecorder.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
{
    DateTime deadline = DateTime.UtcNow + timeout;
    while (!condition())
    {
        if (DateTime.UtcNow >= deadline)
        {
            throw new TimeoutException("Condition was not met before timeout.");
        }

        await Task.Delay(20);
    }
}

static class TestRunner
{
    public static async Task<int> RunAsync(params (string Name, Func<Task> Test)[] tests)
    {
        int failed = 0;
        foreach ((string name, Func<Task> test) in tests)
        {
            try
            {
                await test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {name}: {exception}");
            }
        }

        Console.WriteLine($"RESULT total={tests.Length} failed={failed}");
        return failed == 0 ? 0 : 1;
    }
}

static class Assert
{
    public static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
    public static void False(bool value) { if (value) throw new InvalidOperationException("Expected false."); }
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    public static void Contains(string expected, string actual) { if (!actual.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected text to contain: {expected}"); }
    public static void FileExists(string path) { if (!File.Exists(path)) throw new FileNotFoundException("Expected file was not created.", path); }
    public static void SequenceEqual(byte[] expected, byte[] actual) { if (!expected.SequenceEqual(actual)) throw new InvalidOperationException("Byte sequences differ."); }
}

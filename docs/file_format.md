# Capture File Format

Each prepared session writes four files below its capture directory. The recorder does not parse radar frames or add packet delimiters to `raw.dat`.

## raw.dat

UDP payloads are appended verbatim in receive order:

```text
[UDP packet 0][UDP packet 1][UDP packet 2]...
```

Packet boundaries are reconstructed with `Length` and `FileOffset` from `packets.csv`.

## packets.csv

```csv
Index,Utc,MonoNs,SessionElapsedNs,SenderIp,SenderPort,Length,FileOffset
0,2026-06-12T20:30:05.0012345Z,128001234500,434500,192.168.0.100,23480,1400,0
```

| Field | Meaning |
| --- | --- |
| `Index` | UDP receive index starting at zero |
| `Utc` | Recorder host receive UTC in ISO 8601 format |
| `MonoNs` | Recorder-process monotonic time in nanoseconds |
| `SessionElapsedNs` | `MonoNs - recording_started_monoNs` |
| `SenderIp`, `SenderPort` | UDP source endpoint |
| `Length` | Payload length in bytes |
| `FileOffset` | Payload starting offset in `raw.dat` |

The packet CSV writer flushes every 100 data rows. This bounds the amount of packet-index data that can remain only in process buffers during recording while avoiding a costly flush for every UDP packet. Stop always flushes the remaining rows and closes the file.

## events.csv

```csv
Index,Utc,MonoNs,SessionElapsedNs,Event,Value
```

Events are `prepare_command_received`, `prepared`, `start_command_received`, `recording_started`, `first_packet_received`, `stop_command_received`, `last_packet_received`, `raw_file_closed`, `stopped`, and `error`. Events before recording begins have an empty `SessionElapsedNs`.

## metadata.json

The final metadata contains:

- software name and version
- session ID and capture directory
- master time-base values and caller metadata
- TCP control endpoint
- UDP network configuration
- UTC and monotonic time policy
- generated file names
- packet/byte totals and first, last, and raw-close UTC timestamps

`metadata.json` is finalized during stop. A successful `ok:true,state:"stopped"` response means all four files have been flushed and closed and can be opened by another process. The summary counts only packets actually appended to `raw.dat`; `lastPacketUtc` is the timestamp of the last such packet.

## Offline Processing

Use `packets.csv` to split `raw.dat` back into UDP payloads. Use `SessionElapsedNs` as the preferred recorder-internal relative timeline. Use UTC and the master controller's command send/ack times when aligning radar data with another computer or acquisition program.

# TCP JSON Lines Control Protocol

The control service uses TCP, UTF-8, and one JSON object per line. Every request receives exactly one JSON response line, including malformed commands and commands rejected by the state machine.

The default endpoint is `127.0.0.1:7070`. Start and stop the service from the GUI. Commands are serialized globally so concurrent clients cannot race `start` and `stop`.

## State Machine

| State | Allowed commands |
| --- | --- |
| `idle` | `status`, `prepare` |
| `prepared` | `status`, `start` |
| `recording` | `status`, `stop` |
| `stopping` | `status` |
| `stopped` | `status`, `prepare` |
| `error` | `status` |

An invalid transition returns `ok:false`, `error:"invalid_state"`, the current state, UTC, and `monoNs`.

## Commands

### status

```json
{"cmd":"status"}
```

The response includes state, session ID, capture directory, packet count, byte count, first/last packet UTC, current UTC, and recorder monotonic time.

### prepare

```json
{
  "cmd": "prepare",
  "sessionId": "session_20260612_153000",
  "outputDir": "records/session_20260612_153000/raw/hr23_radar",
  "timeBase": {
    "master": "debug_monitor",
    "prepareCmdSendEpochS": 1781188199.9,
    "prepareCmdSendPerfS": 12345.5
  },
  "metadata": {
    "experimentNote": "example",
    "operator": "",
    "source": "debug_monitor"
  }
}
```

`recordingStartEpochS` may be supplied inside `timeBase`, but it is optional. Prepare creates the output directory and `events.csv`, then records `prepare_command_received` and `prepared`.

### start

```json
{"cmd":"start"}
```

Start records `start_command_received`, creates `raw.dat` and `packets.csv`, establishes the session elapsed-time origin, records `recording_started`, and changes state to `recording` before starting the UDP receive loop. The receiver therefore cannot accept a packet while the recorder still reports `prepared`, which prevents a startup first-packet race.

### stop

```json
{"cmd":"stop"}
```

Stop first changes state to `stopping`, so newly delivered packets are rejected. It cancels UDP reception and waits for the receive loop and any in-progress packet handler to finish before closing writers. It then flushes and closes the raw and packet files, writes `metadata.json`, writes the final events, closes `events.csv`, and only then responds with `ok:true,state:"stopped"`.

A successful `ok:true,state:"stopped"` response is the file-readiness boundary: `raw.dat`, `packets.csv`, `events.csv`, and `metadata.json` have all been flushed and closed.

## Error Response

```json
{
  "ok": false,
  "cmd": "start",
  "state": "idle",
  "error": "invalid_state",
  "message": "start is only allowed in Prepared state",
  "utc": "2026-06-12T20:30:05.0001234Z",
  "monoNs": 128000123400
}
```

Other error codes include `invalid_json`, `invalid_request`, `unknown_command`, and `operation_failed`.

## Time Synchronization

`utc` is host receive time from `DateTimeOffset.UtcNow` and is the coarse cross-program/cross-computer alignment value. `monoNs` is derived from `Stopwatch.GetTimestamp()` and is only meaningful inside this recorder process. It is not an absolute timestamp.

For cross-computer synchronization, use UTC together with the controlling system's command send/ack timestamps. For radar-internal offline processing, use `SessionElapsedNs`, whose zero is the recorder's `recording_started` event.

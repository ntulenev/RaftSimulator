# RaftSimulator

RaftSimulator is a small raft election simulator with HTTP-based node-to-node RPC.
It focuses on leader election and heartbeat stability for local testing scenarios.

## How it works
1. Each node hosts HTTP endpoints for request-vote and append-entries RPC.
2. Nodes start as followers and trigger elections when the timeout elapses.
3. Candidates request votes from peers and become leader on majority.
4. Leaders send heartbeats to keep followers stable.
5. Status snapshots are available via `/raft/status`.

## Configuration (`appsettings.json`)
All options live under the `Raft` section.

- `NodeId` (`int`, required): Node identifier.
- `Port` (`int`, required): Local HTTP port.
- `Peers` (`string`, required): Semicolon-delimited list of peers in `id=http://host:port` format.
- `HeartbeatSeconds` (`int`, optional): Heartbeat interval; default `1`.
- `MinElectionSeconds` (`int`, optional): Minimum election timeout; default `4`.
- `MaxElectionSeconds` (`int`, optional): Maximum election timeout; default `7`.
- `MinNetworkDelaySeconds` (`int`, optional): Minimum simulated network delay; default `1`.
- `MaxNetworkDelaySeconds` (`int`, optional): Maximum simulated network delay; default `2`.

## Example configuration
```json
{
  "Raft": {
    "NodeId": 1,
    "Port": 5001,
    "Peers": "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003;4=http://localhost:5004;5=http://localhost:5005",
    "HeartbeatSeconds": 1,
    "MinElectionSeconds": 4,
    "MaxElectionSeconds": 7,
    "MinNetworkDelaySeconds": 1,
    "MaxNetworkDelaySeconds": 2
  }
}
```

## Running multiple nodes locally
Use environment variables to override per-node configuration.
Example for five nodes:

```bash
# Node 1
RAFT_NodeId=1 RAFT_Port=5001 dotnet run --project RaftSimulator
# Node 2
RAFT_NodeId=2 RAFT_Port=5002 dotnet run --project RaftSimulator
# Node 3
RAFT_NodeId=3 RAFT_Port=5003 dotnet run --project RaftSimulator
# Node 4
RAFT_NodeId=4 RAFT_Port=5004 dotnet run --project RaftSimulator
# Node 5
RAFT_NodeId=5 RAFT_Port=5005 dotnet run --project RaftSimulator
```

## Architecture
- `Abstractions`: interfaces used across layers.
- `Models`: domain and configuration models.
- `Logic`: raft state machine and hosted service.
- `Transport`: HTTP client and DTO mapping.
- `API`: HTTP endpoint mapping.
- `Presentation`: console logging.
  - Uses `Spectre.Console` for formatted output.

## Tests
```bash
dotnet test
```

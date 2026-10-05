---
title: SimpleCluster demo
order: 5
---

Runs a **3-node Raft cluster** on one machine with TCP Raft RPC and a small client API per node.

## Run

```bash
dotnet run --project samples/SimpleCluster/SimpleCluster.csproj
```

## Client API (any node port 7001–7003)

```text
SET color blue
GET color
STATUS
```

Example:

```bash
nc 127.0.0.1 7001
SET greeting hello
GET greeting
```

Raft ports: `9001`, `9002`, `9003`. Data is stored under `data/n1`, `data/n2`, `data/n3`.

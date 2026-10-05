---
title: Quickstart
order: 1
---

## Reference the library

```bash
dotnet add package SparkRaftSharp
# or project reference:
dotnet add reference path/to/SparkRaftSharp.csproj
```

## Minimal in-process cluster

```csharp
using System.Text;
using SparkRaftSharp;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;
using SparkRaftSharp.Transport;

var transport = new InProcessRaftTransport();
var peers = new[]
{
    new RaftPeer(new NodeId("a"), "a"),
    new RaftPeer(new NodeId("b"), "b"),
    new RaftPeer(new NodeId("c"), "c"),
};

IRaftNode CreateNode(NodeId id, IRaftStateMachine machine) =>
    new RaftNodeBuilder()
        .WithCluster(new ClusterConfiguration(id, peers))
        .WithTransport(transport)
        .WithStateMachine(machine)
        .Build();

var nodes = new[] { CreateNode(peers[0].Id, machineA), /* ... */ };
foreach (var n in nodes)
    await n.StartAsync();

var leader = /* wait until one node reports RaftRole.Leader */;
var index = await leader.ProposeAsync(Encoding.UTF8.GetBytes("hello"));
```

## Production hosting

1. Implement `IRaftTransport` over your RPC layer (gRPC, TCP, etc.).
2. Optionally replace `InMemoryLogStore` / `InMemoryStateStore` with durable implementations.
3. Implement `IRaftStateMachine` to apply committed commands to your domain model.
4. Expose `NotLeaderException.KnownLeader` to clients for redirect/retry.

See [architecture.md](architecture.md) for extension points.

## Clustered server demo

```bash
dotnet run --project samples/SimpleCluster/SimpleCluster.csproj
```

Then use `nc 127.0.0.1 7001` with `SET`, `GET`, and `STATUS`. See [simple-cluster.md](simple-cluster.md).

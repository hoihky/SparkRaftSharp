---
title: Raft protocol guide
order: 2
---

This page explains how Raft works as a consensus protocol and how SparkRaftSharp maps each idea to types and services in the library. The goal is to help you reason about failures, leadership changes, and where your application code plugs in.

## Why clusters need consensus

A replicated service keeps several copies of the same data so it can survive machine loss. The hard part is agreeing on **one ordered history of changes** when messages are delayed, duplicated, or nodes crash. Consensus picks a single serial history that every correct node eventually applies.

Raft solves that problem by electing a **leader** for stretches of time. The leader is the only node that may accept new commands from clients. It appends commands to a replicated **log**; followers copy that log. Once a command sits on a majority of nodes, it is **committed** and can be applied to your application state.

## Roles and terms

Each node is a **follower**, **candidate**, or **leader**.

| Role | Responsibility |
|------|----------------|
| Follower | Replies to the leader; does not accept client writes. Starts an election if the leader goes quiet too long. |
| Candidate | Requests votes after an election timeout. Becomes leader if it wins a quorum. |
| Leader | Accepts proposals, appends to its log, and pushes entries to followers. |

Time is divided into **terms**—monotonic integers. At most one leader per term. When a node hears about a newer term, it refreshes its view and steps down if it was leading.

In SparkRaftSharp:

- `RaftRole` models the three roles.
- `RaftTerm` is a typed wrapper for the term counter.
- `PersistentState.CurrentTerm` and `RaftNodeStatus` expose term and role to hosts.

## The replicated log

The log is an array of entries. Each entry has an **index** (1-based in this library), the **term** when it was created, and an opaque **payload** (`byte[]`) for your app.

Raft never forks committed history: if two entries share an index, they share the same term. Leaders only append; conflicting suffixes on followers are deleted before new entries are copied in.

SparkRaftSharp types:

- `RaftEntry` — index, term, data.
- `LogIndex` — avoids mixing indices with other numbers.
- `ILogStore` — append, read, truncate suffix; implementations include `InMemoryLogStore` and `FileLogStore`.

## Election flow

When a follower’s election timer fires, it increments its term, votes for itself, and sends **RequestVote** RPCs to peers. A peer grants at most one vote per term and only if the candidate’s log is at least as up-to-date as its own (compare last term, then last index).

If the candidate receives votes from a **quorum** (`ClusterConfiguration.QuorumSize`), it becomes leader, initializes per-follower progress counters, and begins heartbeats.

Implementation mapping:

- `ElectionService` — starts elections, handles vote requests and responses.
- `ElectionTimeout` in `RaftOptions` plus `IElectionTimeoutJitter` / `RandomElectionTimeoutJitter` — spreads timeouts to reduce split votes.
- `RequestVoteRequest` / `RequestVoteResponse` — wire messages; your `IRaftTransport` delivers them.

## Heartbeats and AppendEntries

The leader sends **AppendEntries** RPCs often (even with zero new entries) so followers reset their election timers. Each RPC carries:

- the leader’s term and id;
- **prev log index / term** — consistency check point;
- zero or more new entries;
- **leader commit index** — hint for followers to advance their commit pointer.

If the follower’s log disagrees at the previous point, it rejects the RPC. The leader then walks backward on that follower’s `nextIndex` until logs align.

SparkRaftSharp:

- `AppendEntriesService` — follower-side validation, append, commit hint.
- `ReplicationService` — leader-side replication and snapshot fallback.
- `AppendEntriesRequest` / `AppendEntriesResponse` — message DTOs.
- Leader state: `NextIndex` and `MatchIndex` dictionaries on `RaftRuntime`.

## Commitment and application

An entry is **committed** once the leader knows it is stored on a majority of servers for its current term. The leader updates `commitIndex`; followers learn it through AppendEntries. Nodes apply entries in order where `lastApplied < commitIndex`, calling your state machine.

Client proposals in this library complete when the entry is **committed** (not necessarily after every follower has applied).

Components:

- `LogCommitService.TryAdvanceCommitIndex` — quorum check on the leader.
- `LogCommitService.ApplyCommittedAsync` — drives `IRaftStateMachine.ApplyAsync`.
- `IRaftNode.ProposeAsync` — leader-only; queues `ProposeEvent` on the engine channel.
- `NotLeaderException` — thrown on followers; includes `KnownLeader` when known for client redirect.

## Persistent vs volatile state

| State | Examples | SparkRaftSharp |
|-------|----------|----------------|
| Persistent | current term, votedFor, log entries, snapshot metadata | `IStateStore`, `ILogStore`, optional `ISnapshotStore` |
| Volatile | commit index, last applied, leader id, match/next indices | fields on `RaftRuntime`, rebuilt on leadership |

On startup, `RaftEngine` loads `IStateStore` and replays apply for any committed-but-not-applied entries.

## Snapshots

Long logs are compacted by writing a **snapshot** that captures state up to index *N*. Followers far behind can receive **InstallSnapshot** instead of a long chain of AppendEntries.

SparkRaftSharp provides:

- `RaftSnapshot`, `ISnapshotStore`, `InMemorySnapshotStore`.
- `InstallSnapshotRequest` / `InstallSnapshotResponse`.
- `InstallSnapshotService` on followers; leader sends snapshots when `ReplicationService` detects a follower is before the latest snapshot.
- `IRaftStateMachine.ApplySnapshotAsync` — rebuild local state from snapshot bytes.

Full automatic compaction on the leader is still evolving; hosts can populate `ISnapshotStore` for lagging peers.

## Engine architecture

All consensus mutations run on a **single event loop** inside `RaftEngine`:

1. Transport callbacks enqueue `InboundMessageEvent`.
2. Timers enqueue election or heartbeat ticks.
3. `ProposeAsync` enqueues `ProposeEvent`.

This design avoids fine-grained locking on role and log mutations. Services (`ElectionService`, `AppendEntriesService`, `ReplicationService`, `LogCommitService`, `InstallSnapshotService`) share a `RaftRuntime` bag and are orchestrated from `DispatchAsync`.

Public surface:

- `RaftNodeBuilder` — wires cluster config, stores, transport, state machine, options.
- `IRaftNode` — `StartAsync`, `ProposeAsync`, `ResignLeadershipAsync`, `GetStatus`.
- `IRaftObserver` — optional role, leader, and commit notifications.

## Safety properties (informal)

These are the guarantees Raft is built for; SparkRaftSharp follows the same rules:

1. **Election safety** — at most one leader per term.
2. **Leader append-only** — leaders never delete or overwrite their own log tail; they only append.
3. **Log matching** — if two logs contain an entry with the same index and term, they are identical up to that index.
4. **Leader completeness** — a committed entry appears in every future leader’s log for that term.
5. **State machine safety** — if a node applies an entry at index *i*, no other node applies a different entry at *i*.

Violations usually mean a bug, clock misuse, or a transport that delivers stale RPCs without term checks—handlers always compare the RPC term with `CurrentTerm` first.

## Operating a host application

Typical integration steps:

1. Define static membership (`ClusterConfiguration` + `RaftPeer` addresses your transport understands).
2. Implement `IRaftTransport` (TCP sample in SimpleCluster, in-process for tests).
3. Implement `IRaftStateMachine` for business logic on committed bytes.
4. Choose stores (`InMemory*` for tests, `File*` for durability experiments).
5. Start nodes, wait for a leader, call `ProposeAsync` on the leader (or follow `NotLeaderException.KnownLeader`).

Tune `RaftOptions.ElectionTimeoutMin/Max` and `HeartbeatInterval` for your network latency. Heartbeats must be noticeably shorter than election timeouts.

## RPC summary

| Message | Direction | Purpose |
|---------|-----------|---------|
| RequestVote | candidate → peer | ask for vote in current term |
| RequestVoteResponse | peer → candidate | grant or deny |
| AppendEntries | leader → follower | replicate log + heartbeat |
| AppendEntriesResponse | follower → leader | success + match index |
| InstallSnapshot | leader → follower | ship compacted state |
| InstallSnapshotResponse | follower → leader | acknowledge install |

All implement `IRaftMessage` with a `RaftMessageKind` discriminator for routing in your transport codec.

## Further reading in this repo

- [Architecture](architecture.md) — layering and dependency direction.
- [Quickstart](quickstart.md) — minimal wiring example.
- [SimpleCluster demo](simple-cluster.md) — TCP transport and client API.

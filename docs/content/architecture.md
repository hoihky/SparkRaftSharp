---
title: Architecture
order: 3
---

## Overview

The library separates **consensus core** (Raft algorithm), **persistence**, **networking**, and **application state machine**. Host applications reference `SparkRaftSharp` and supply transport and optional custom stores.

For a full walkthrough of Raft behavior and how each RPC maps to services in this repo, see the [Raft protocol guide](raft-protocol.md).

## Layers

### Public API

- **`RaftNodeBuilder`** — constructs `IRaftNode` with validated options.
- **`IRaftNode`** — lifecycle, `ProposeAsync`, status (`Role`, `LeaderId`, `CommitIndex`).
- **`IRaftStateMachine`** — apply committed entries and snapshots.

### Consensus engine (internal)

Service-oriented components for elections, RPC handling, replication, and commit advancement.

### Persistence

`ILogStore`, `IStateStore`, optional `ISnapshotStore`, plus in-memory and file-backed implementations.

### Transport

`IRaftTransport` with `InProcessRaftTransport` for tests; hosts supply production networking.

## Threading

Single logical actor (channel queue) for engine state; transport and timers enqueue events.

## Security

Encryption and authentication are host responsibilities.

---
title: Features
order: 4
---

## Goals

A composable .NET Raft library for strongly consistent clustered applications.

## Core protocol (P0)

Leader election, log replication, safety properties, heartbeats, commitment, state machine apply.

## Developer experience (P0–P1)

`IRaftNode`, `RaftNodeBuilder`, typed primitives, async API, observers, logging hooks.

## Extensibility

Pluggable log, state, transport, and state machine; RPC services split by responsibility.

## Milestones

- **v0.1** — MVP cluster, in-proc transport, propose/apply
- **v0.2+** — snapshots, pre-vote, membership (planned)

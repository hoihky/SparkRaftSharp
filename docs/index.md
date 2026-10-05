---
title: Overview
order: 0
---

<div class="hero">
  <div class="hero-icon" aria-hidden="true">SR</div>
  <div class="hero-text">
    <h1>SparkRaftSharp</h1>
    <p class="hero-tagline">
      A .NET Raft consensus library for building highly available, strongly consistent clustered servers.
      Compose storage, transport, and your state machine behind a small public API.
    </p>
    <div class="hero-links">
      <a href="quickstart.md">Quickstart</a>
      <a href="architecture.md">Architecture</a>
      <a href="simple-cluster.md">Cluster demo</a>
    </div>
  </div>
</div>

## What you get

<div class="career-grid">
  <article class="career-card">
    <h3>Pluggable design</h3>
    <p>Swap log stores, durable state, transport, and application logic without forking the consensus core.</p>
  </article>
  <article class="career-card">
    <h3>Developer-first API</h3>
    <p><code>RaftNodeBuilder</code>, <code>IRaftNode.ProposeAsync</code>, typed indices and terms, and clear leader redirect errors.</p>
  </article>
  <article class="career-card">
    <h3>Runnable demo</h3>
    <p>The SimpleCluster sample runs a three-node TCP cluster with a line-oriented client API for SET/GET commands.</p>
  </article>
</div>

## Documentation map

| Page | Description |
|------|-------------|
| [Quickstart](quickstart.md) | Reference the package and wire an in-process cluster |
| [Architecture](architecture.md) | Layers, patterns, and extension points |
| [Features](features.md) | Capability matrix and milestones |
| [Roadmap](roadmap.md) | Delivery phases and status |
| [SimpleCluster demo](simple-cluster.md) | TCP clustered server sample |

## Status

Current milestone: **v0.1** — elections, replication, snapshots (install path), file-backed stores, and the SimpleCluster demo. See the [roadmap](roadmap.md) for Phase 7 plans.

## HTML site

Run `./generate-docs.sh` from the repository root (requires MDWeb as a sibling clone). That writes a staging build to `docs/html/`, copies HTML and `assets/` into this folder, and updates `docs/.nojekyll` so GitHub Pages serves the static site instead of Jekyll. **Commit** `.nojekyll`, `*.html`, and `assets/` after regenerating so [the project site](https://hoihky.github.io/SparkRaftSharp/) picks up the theme.

---
title: Overview
order: 0
---

<section class="hero" id="top">
  <div class="hero-mark" aria-hidden="true">SR</div>
  <div class="hero-text">
    <h1>SparkRaftSharp</h1>
    <p class="hero-tagline">
      A .NET Raft consensus library for building highly available, strongly consistent clustered servers.
      Compose storage, transport, and your state machine behind a small public API.
    </p>
    <ul class="skill-list" aria-label="Technologies">
      <li>.NET 9</li>
      <li>Raft consensus</li>
      <li>Pluggable transport</li>
      <li>Cluster demo</li>
    </ul>
    <div class="hero-links">
      <a href="quickstart.html">Quickstart</a>
      <a href="raft-protocol.html">Raft guide</a>
      <a href="architecture.html">Architecture</a>
      <a href="simple-cluster.html">Cluster demo</a>
    </div>
  </div>
</section>

<p class="disclaimer">
  <strong>Disclaimer:</strong> This project is an experimental, work-in-progress prototype built with the help of
  &ldquo;vibe coding&rdquo;. Things will break. Features are currently missing, and the build scripts might not work at all.
  Please be aware that it may not be stable enough for production use now.
</p>

<section id="highlights">
  <h2 class="section-title">What you get</h2>
  <div class="pillar-grid">
    <article class="pillar-card">
      <h3>Pluggable design</h3>
      <p>Swap log stores, durable state, transport, and application logic without forking the consensus core.</p>
    </article>
    <article class="pillar-card">
      <h3>Developer-first API</h3>
      <p><code>RaftNodeBuilder</code>, <code>IRaftNode.ProposeAsync</code>, typed indices and terms, and clear leader redirect errors.</p>
    </article>
    <article class="pillar-card">
      <h3>Runnable demo</h3>
      <p>The SimpleCluster sample runs a three-node TCP cluster with a line-oriented client API for SET/GET commands.</p>
    </article>
  </div>
</section>

<section id="docs-map">
  <h2 class="section-title">Documentation map</h2>
  <table>
    <thead>
      <tr><th>Page</th><th>Description</th></tr>
    </thead>
    <tbody>
      <tr><td><a href="quickstart.html">Quickstart</a></td><td>Reference the package and wire an in-process cluster</td></tr>
      <tr><td><a href="raft-protocol.html">Raft protocol guide</a></td><td>Consensus concepts and SparkRaftSharp components</td></tr>
      <tr><td><a href="architecture.html">Architecture</a></td><td>Layers, patterns, and extension points</td></tr>
      <tr><td><a href="features.html">Features</a></td><td>Capability matrix and milestones</td></tr>
      <tr><td><a href="roadmap.html">Roadmap</a></td><td>Delivery phases and status</td></tr>
      <tr><td><a href="simple-cluster.html">SimpleCluster demo</a></td><td>TCP clustered server sample</td></tr>
    </tbody>
  </table>
</section>

<section id="status">
  <h2 class="section-title">Status</h2>
  <p>
    Current milestone: <strong>v0.1</strong> — elections, replication, snapshots (install path), file-backed stores, and the SimpleCluster demo.
    See the <a href="roadmap.html">roadmap</a> for Phase 7 plans.
  </p>
</section>

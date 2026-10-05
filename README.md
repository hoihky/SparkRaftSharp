# SparkRaftSharp

Raft consensus library for .NET — build highly available, strongly consistent clustered servers with a composable API (`IRaftNode`, pluggable transport, log, and state machine).

> **Disclaimer:** This project is an experimental, work-in-progress prototype built with the help of “vibe coding”. Things will break. Features are currently missing, and the build scripts might not work at all. Please be aware that it may not be stable enough for production use now.

## Documentation

- **Online:** [https://hoihky.github.io/SparkRaftSharp/](https://hoihky.github.io/SparkRaftSharp/)
- **Source:** Markdown in `docs/content/`; static site via `./generate-docs.sh` (MDWeb)

## Quick start

```bash
dotnet build
dotnet test
dotnet run --project samples/SimpleCluster/SimpleCluster.csproj
```

See [docs/content/quickstart.md](docs/content/quickstart.md) for library usage.

## License

MIT (see repository license file when published).

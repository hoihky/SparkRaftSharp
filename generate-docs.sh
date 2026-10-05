#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MDWEB_CLI="${MDWEB_CLI:-${REPO_ROOT}/../MDWeb/src/MDWeb.Cli}"

if [[ ! -f "${MDWEB_CLI}/MDWeb.Cli.csproj" ]]; then
  echo "MDWeb CLI not found. Clone MDWeb as a sibling of this repository or set MDWEB_CLI." >&2
  exit 1
fi

dotnet run --project "${MDWEB_CLI}" \
  --source "${REPO_ROOT}/docs" \
  --output "${REPO_ROOT}/docs/html" \
  --theme "${REPO_ROOT}/.mdweb/theme" \
  --title "SparkRaftSharp" \
  --description "Raft consensus library for .NET clustered applications." \
  --no-fix-markdown-links \
  --footer "<p>SparkRaftSharp documentation — generated with MDWeb.</p>"

echo "Site written to docs/html/"

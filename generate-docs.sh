#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MDWEB_CLI="${MDWEB_CLI:-${REPO_ROOT}/../MDWeb/src/MDWeb.Cli}"
BUILD_DIR="${REPO_ROOT}/docs/html"
PUBLISH_DIR="${REPO_ROOT}/docs"

if [[ ! -f "${MDWEB_CLI}/MDWeb.Cli.csproj" ]]; then
  echo "MDWeb CLI not found. Clone MDWeb as a sibling of this repository or set MDWEB_CLI." >&2
  exit 1
fi

dotnet run --project "${MDWEB_CLI}" \
  --source "${PUBLISH_DIR}/content" \
  --output "${BUILD_DIR}" \
  --theme "${REPO_ROOT}/.mdweb/theme" \
  --title "SparkRaftSharp" \
  --description "Raft consensus library for .NET clustered applications." \
  --no-fix-markdown-links \
  --footer "<p>SparkRaftSharp documentation — generated with MDWeb.</p>"

# GitHub Pages serves the /docs folder with Jekyll by default, which ignores our theme.
# Publish static HTML + assets beside the Markdown sources and disable Jekyll.
touch "${PUBLISH_DIR}/.nojekyll"

while IFS= read -r -d '' file; do
  rel="${file#${BUILD_DIR}/}"
  dest="${PUBLISH_DIR}/${rel}"
  mkdir -p "$(dirname "${dest}")"
  cp "${file}" "${dest}"
done < <(find "${BUILD_DIR}" -type f -print0)

echo "Site built in docs/html/ and published to docs/ (HTML + assets)."
echo "Commit docs/.nojekyll, docs/*.html, and docs/assets/ for GitHub Pages."

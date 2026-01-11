#!/usr/bin/env bash
set -euo pipefail

# Run unit tests with Coverlet and print coverage summary to terminal.
# Outputs Cobertura XML under ./coverage/

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")"/.. && pwd)"
SOLUTION="$ROOT_DIR/be-nexus-fs/be-nexus-fs.sln"
OUT_DIR="$ROOT_DIR/coverage"

mkdir -p "$OUT_DIR"

echo "Restoring solution..."
dotnet restore "$SOLUTION"

echo "Running tests with coverage..."
dotnet test "$SOLUTION" \
  --configuration Debug \
  --verbosity normal \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$OUT_DIR/" \
  /p:CoverletOutputFormat=cobertura \
  /p:ExcludeByFile='**/NexusFS.Tests/**/*.cs'

coverage_xml="$OUT_DIR/coverage.cobertura.xml"
# Coverlet prints a summary in the test output; point to the file for convenience
if [[ -f "$coverage_xml" ]]; then
  echo "\nCoverage XML: $coverage_xml"
fi

# If reportgenerator is available, produce a detailed HTML report
if command -v reportgenerator >/dev/null 2>&1; then
  HTML_DIR="$OUT_DIR/html"
  mkdir -p "$HTML_DIR"
  echo "Generating HTML coverage report..."
  reportgenerator -reports:"$coverage_xml" -targetdir:"$HTML_DIR" -reporttypes:"Html;TextSummary"
  if [[ -f "$HTML_DIR/index.html" ]]; then
    echo "HTML report: $HTML_DIR/index.html"
  fi
else
  echo "(Optional) To generate HTML, run:"
  echo "  dotnet new tool-manifest && dotnet tool install dotnet-reportgenerator-globaltool"
  echo "Then re-run this script."
fi

echo "Done."
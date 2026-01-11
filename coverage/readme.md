# Code Coverage

This guide shows how to generate code coverage locally, print a summary to the terminal, and produce an HTML report.

## Quickstart

Run the following commands from the repository root:

```bash
#!/usr/bin/env bash
set -euo pipefail

# 1) Ensure a local dotnet tool manifest exists and install ReportGenerator
dotnet new tool-manifest || true
dotnet tool install dotnet-reportgenerator-globaltool || true

# 2) Restore the solution
dotnet restore be-nexus-fs/be-nexus-fs.sln

# 3) Run tests with coverage (Coverlet via MSBuild)
#    - Prints a coverage summary to the terminal
#    - Writes Cobertura XML to ./coverage/coverage.cobertura.xml
#    - Excludes test project files from coverage via ExcludeByFile
dotnet test be-nexus-fs/be-nexus-fs.sln \
  --verbosity normal \
  /p:CollectCoverage=true \
  /p:CoverletOutput=./coverage/ \
  /p:CoverletOutputFormat=cobertura \
  /p:ExcludeByFile="**/NexusFS.Tests/**/*.cs"

# 4) Generate an HTML report and a text summary using ReportGenerator
dotnet tool run reportgenerator \
  -reports:"coverage/coverage.cobertura.xml" \
  -targetdir:"coverage/html" \
  -reporttypes:"Html;TextSummary"

echo "\nCoverage XML: coverage/coverage.cobertura.xml"
echo "HTML report: coverage/html/index.html"
```

## Alternate: Script

Use the project script if you prefer:

```bash
bash scripts/test-coverage.sh
```

Outputs:
- Terminal summary during `dotnet test`
- XML: `coverage/coverage.cobertura.xml`
- HTML: `coverage/html/index.html` (when ReportGenerator is installed)

## Notes

- Requires .NET SDK 9.
- Exclusions: the quickstart excludes test source files using `ExcludeByFile`. You can adjust patterns if needed.
- To open the HTML report on Linux: `xdg-open coverage/html/index.html`.

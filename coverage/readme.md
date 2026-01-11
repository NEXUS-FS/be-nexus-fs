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
#    - Writes Cobertura XML to be-nexus-fs/coverage/coverage.cobertura.xml
#      (the path is relative to each project under be-nexus-fs/)
#    - Excludes test files and hard-to-test external providers via ExcludeByFile
dotnet test \
  --verbosity normal \
  /p:CollectCoverage=true \
  /p:CoverletOutput=../coverage/ \
  /p:CoverletOutputFormat=cobertura \
  /p:ExcludeByFile="**/NexusFS.Tests/**/*.cs;**/Infrastructure/Services/GoogleDriveApiClient.cs;**/Infrastructure/Services/NexusApi.cs;**/Infrastructure/Services/S3Provider.cs;**/Infrastructure/Services/FtpProvider.cs;**/Infrastructure/Services/WebDAVProvider.cs;**/Infrastructure/Services/Decorators/RedisProviderDecorator.cs;**/Infrastructure/Services/Security/MCPServerProxy.cs"

# 4) Generate a text summary (fast) or HTML report using ReportGenerator
#    NOTE: use the file produced under be-nexus-fs/coverage/ (not repo-root/coverage/)
#    Text-only:
dotnet tool run reportgenerator \
  -reports:"be-nexus-fs/coverage/coverage.cobertura.xml" \
  -targetdir:"be-nexus-fs/coverage/text" \
  -reporttypes:"TextSummary"

#    HTML (optional):
dotnet tool run reportgenerator \
  -reports:"be-nexus-fs/coverage/coverage.cobertura.xml" \
  -targetdir:"be-nexus-fs/coverage/html" \
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

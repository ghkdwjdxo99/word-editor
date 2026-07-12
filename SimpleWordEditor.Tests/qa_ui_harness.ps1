$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'UiHarness\SimpleWordEditor.UiHarness.csproj'
dotnet run --project $project
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

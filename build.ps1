param([switch]$Locked)
$ErrorActionPreference='Stop'
$restoreArgs=@();if($Locked){$restoreArgs+='--locked-mode'}
foreach($project in @('desktop/BD2Territory.Desktop.csproj','tests/BD2Territory.Tests.csproj','compatibility-cli/BD2Territory.Compatibility.Cli.csproj','handoff-tests/BD2Territory.HandoffTests.csproj')){
 & dotnet restore (Join-Path $PSScriptRoot $project) @restoreArgs --nologo
 if($LASTEXITCODE -ne 0){throw "Restore failed: $project"}
}
& dotnet build (Join-Path $PSScriptRoot 'desktop/BD2Territory.Desktop.csproj') -c Release --no-restore --nologo -v minimal
if($LASTEXITCODE -ne 0){throw 'Desktop build failed'}
& dotnet run --project (Join-Path $PSScriptRoot 'tests/BD2Territory.Tests.csproj') -c Release --no-restore
if($LASTEXITCODE -ne 0){throw 'Batch and transaction regression failed'}

& dotnet run --project (Join-Path $PSScriptRoot 'handoff-tests/BD2Territory.HandoffTests.csproj') -c Release --no-restore
if($LASTEXITCODE -ne 0){throw 'Runtime handoff lifecycle regression failed'}

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$executable = Join-Path $root 'bin\Release\net8.0\AtlasLetreros.exe'

if (-not (Test-Path -LiteralPath $executable)) {
    & dotnet build (Join-Path $root 'AtlasLetreros.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$process = Start-Process -FilePath $executable -ArgumentList '--urls','http://127.0.0.1:5114' -WorkingDirectory $root -WindowStyle Hidden -PassThru
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5114/api/status' -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'ASP.NET no inició para las pruebas E2E.' }
    & (Join-Path $root 'node_modules\.bin\playwright.cmd') test
    exit $LASTEXITCODE
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
}

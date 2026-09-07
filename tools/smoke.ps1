param([string]$ChromePath)
$ErrorActionPreference = 'Stop'
$atlasRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$atlasDotnet = Join-Path $atlasRoot '.dotnet/dotnet.exe'
$env:DOTNET_ROOT = Join-Path $atlasRoot '.dotnet'
$env:DOTNET_CLI_HOME = Join-Path $atlasRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$atlasRun = Join-Path $atlasRoot ('artifacts/smoke-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$atlasBuild = Join-Path $atlasRun 'build'
New-Item -ItemType Directory -Path $atlasRun -Force | Out-Null
if (!$ChromePath) {
    $atlasCachedChrome = Join-Path $env:LOCALAPPDATA 'ms-playwright/chromium-1155/chrome-win/chrome.exe'
    if (Test-Path -LiteralPath $atlasCachedChrome) { $ChromePath = $atlasCachedChrome }
}
$atlasPreviousChrome = $env:ATLAS_TEST_CHROME
if ($ChromePath) { $env:ATLAS_TEST_CHROME = $ChromePath }
$atlasServer = $null
Push-Location $atlasRoot
try {
    & node tests/AtlasLetrero.Tests/render-regression.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de lógica JS.' }
    & $atlasDotnet build src/AtlasLetrero.App/AtlasLetrero.App.csproj -c Release --nologo --artifacts-path $atlasBuild --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de la aplicación.' }
    & $atlasDotnet build tests/AtlasLetrero.E2E/AtlasLetrero.E2E.csproj -c Release --nologo --artifacts-path $atlasBuild --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de E2E.' }
    $atlasPortProbe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $atlasPortProbe.Start()
    $atlasPort = $atlasPortProbe.LocalEndpoint.Port
    $atlasPortProbe.Stop()
    $atlasUrl = "http://127.0.0.1:$atlasPort"
    $atlasDll = Join-Path $atlasBuild 'bin/AtlasLetrero.App/release/AtlasLetrero.App.dll'
    $atlasData = Join-Path $atlasRun 'data'
    $atlasArguments = '"{0}" --urls "{1}" --Atlas:DataRoot "{2}"' -f $atlasDll, $atlasUrl, $atlasData
    $atlasServer = Start-Process -FilePath $atlasDotnet -ArgumentList $atlasArguments -WorkingDirectory (Join-Path $atlasRoot 'src/AtlasLetrero.App') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $atlasRun 'server.log') -RedirectStandardError (Join-Path $atlasRun 'server-error.log')
    $atlasReady = $false
    for ($atlasAttempt=0; $atlasAttempt -lt 40; $atlasAttempt++) {
        if ($atlasServer.HasExited) { throw "Servidor de prueba terminado. Revisa $atlasRun" }
        try { if ((Invoke-RestMethod "$atlasUrl/api/health" -TimeoutSec 1).status -eq 'ok') { $atlasReady = $true; break } } catch {}
        Start-Sleep -Milliseconds 250
    }
    if (!$atlasReady) { throw 'El servidor de prueba no respondió.' }
    & $atlasDotnet (Join-Path $atlasBuild 'bin/AtlasLetrero.E2E/release/AtlasLetrero.E2E.dll') $atlasUrl $atlasRun
    if ($LASTEXITCODE -ne 0) { throw "Fallaron las pruebas E2E. Evidencia: $atlasRun" }
    Write-Output "PASS. Evidencia: $atlasRun"
} finally {
    if ($atlasServer -and !$atlasServer.HasExited) { Stop-Process -Id $atlasServer.Id }
    $env:ATLAS_TEST_CHROME = $atlasPreviousChrome
    Pop-Location
}

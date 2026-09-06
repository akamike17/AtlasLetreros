param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$atlasRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$atlasDotnet = Join-Path $atlasRoot '.dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $atlasDotnet)) { throw 'Falta el SDK local .dotnet/dotnet.exe. Instala .NET SDK 8.0.424 en .dotnet.' }
$env:DOTNET_ROOT = Join-Path $atlasRoot '.dotnet'
$env:DOTNET_CLI_HOME = Join-Path $atlasRoot '.dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $atlasRoot '.nuget-cache'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
$atlasUrl = 'http://127.0.0.1:5088'
try {
    $atlasHealth = Invoke-RestMethod "$atlasUrl/api/health" -TimeoutSec 2
    if ($atlasHealth.status -eq 'ok') {
        if (!$NoBrowser) { Start-Process $atlasUrl }
        Write-Output "Atlas Letrero ya esta abierto en $atlasUrl"
        exit 0
    }
} catch { }
Push-Location $atlasRoot
try {
    & $atlasDotnet build AtlasLetrero.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'La compilacion fallo. Revisa los mensajes anteriores.' }
    $atlasApp = Join-Path $atlasRoot 'src/AtlasLetrero.App'
    $atlasLog = Join-Path $atlasRoot '.local'
    New-Item -ItemType Directory -Path $atlasLog -Force | Out-Null
    $atlasProcess = Start-Process -FilePath $atlasDotnet -ArgumentList 'bin/Release/net8.0/AtlasLetrero.App.dll' -WorkingDirectory $atlasApp -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $atlasLog 'server.log') -RedirectStandardError (Join-Path $atlasLog 'server-error.log')
    $atlasProcess.Id | Set-Content (Join-Path $atlasLog 'server.pid')
    $atlasReady = $false
    for ($atlasAttempt=0; $atlasAttempt -lt 40; $atlasAttempt++) {
        if ($atlasProcess.HasExited) { throw "El servidor no pudo iniciar. Revisa $atlasLog\server-error.log" }
        try { $atlasHealth = Invoke-RestMethod "$atlasUrl/api/health" -TimeoutSec 1; if ($atlasHealth.status -eq 'ok') { $atlasReady=$true; break } } catch { }
        Start-Sleep -Milliseconds 250
    }
    if (!$atlasReady) { throw 'El servidor no respondio a tiempo.' }
    if (!$NoBrowser) { Start-Process $atlasUrl }
    Write-Output "Atlas Letrero disponible en $atlasUrl"
} finally { Pop-Location }

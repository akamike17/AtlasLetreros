param([string]$Output = "artifacts/release")
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdk = Join-Path $root '.dotnet/dotnet.exe'
if (!(Test-Path $sdk)) { throw 'Falta .dotnet/dotnet.exe (SDK 8.0.424).' }
$out = [IO.Path]::GetFullPath((Join-Path $root $Output))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$env:DOTNET_ROOT = Join-Path $root '.dotnet'
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages'
& $sdk publish (Join-Path $root 'src/AtlasLetrero.App/AtlasLetrero.App.csproj') -c Release -r win-x64 --self-contained false -o (Join-Path $out 'AtlasLetrero') --nologo
if ($LASTEXITCODE -ne 0) { throw 'La publicación falló.' }
Copy-Item (Join-Path $root 'Abrir Atlas Letrero.cmd') (Join-Path $out 'AtlasLetrero') -Force
Copy-Item (Join-Path $root 'README.md') (Join-Path $out 'AtlasLetrero') -Force
Compress-Archive -Path (Join-Path $out 'AtlasLetrero') -DestinationPath (Join-Path $out 'AtlasLetrero-0.1.0-win-x64.zip') -Force
Write-Output "PASS: paquete publicado en $out"

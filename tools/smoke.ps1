$ErrorActionPreference='Stop'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '../.cache/dotnet-home'; $env:NUGET_PACKAGES=Join-Path $PSScriptRoot '../.cache/nuget'
$app=Join-Path $PSScriptRoot '../src/AtlasLetrero.App/AtlasLetrero.App.csproj'
& (Join-Path $PSScriptRoot '../.dotnet/dotnet.exe') restore $app
& (Join-Path $PSScriptRoot '../.dotnet/dotnet.exe') build $app -c Release
$proc=Start-Process -FilePath (Join-Path $PSScriptRoot '../.dotnet/dotnet.exe') -ArgumentList 'run','-c','Release','--no-build','--project',$app -WorkingDirectory (Join-Path $PSScriptRoot '..') -WindowStyle Hidden -PassThru
try { for($i=0;$i -lt 30;$i++){try{$h=Invoke-RestMethod http://127.0.0.1:5187/api/health;break}catch{Start-Sleep -Milliseconds 250}}; if($h.status -ne 'ok'){throw 'Health check falló.'}; foreach($page in @('/','/projects.html','/editor.html','/device.html')) { $r=Invoke-WebRequest ('http://127.0.0.1:5187'+$page); if($r.StatusCode -ne 200){throw "Página $page falló."}; if($r.Content -match '\[object Promise\]|\[object Object\]'){throw "Placeholder Promise/Object en $page"} }; & (Join-Path $PSScriptRoot 'verify-assets.ps1'); Write-Host 'Smoke: PASS' } finally { Stop-Process $proc.Id -Force -ErrorAction SilentlyContinue }

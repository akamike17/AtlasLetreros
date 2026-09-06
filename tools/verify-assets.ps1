$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot '../src/AtlasLetrero.App/wwwroot'
foreach($type in @('icons','emojis','fonts')){
 $catalog=Get-Content (Join-Path $root "assets/catalogs/$type.json") -Raw | ConvertFrom-Json
 $seen=@{}
 foreach($item in $catalog){if($seen.ContainsKey($item.id)){throw "ID duplicado: $($item.id)"};$seen[$item.id]=$true;if(!(Test-Path (Join-Path $root "assets/$($item.path)"))){throw "Asset faltante: $($item.path)"}}
}
foreach($license in @('bootstrap-icons-LICENSE.txt','twemoji-LICENSE-GRAPHICS.txt','twemoji-code-LICENSE.txt','ATTRIBUTION.txt','font-5x7-LICENSE.txt','font-5x8-LICENSE.txt','font-6x10-LICENSE.txt','font-8x13-LICENSE.txt')){if(!(Test-Path (Join-Path $root "assets/licenses/$license"))){throw "Licencia faltante: $license"}}
$bad=Get-ChildItem $root -Recurse -File | Where-Object {$_.Extension -in '.html','.css','.js'} | Select-String -Pattern 'https?://'
if($bad){throw "URL de runtime detectada: $bad"}
Write-Output 'PASS: assets locales, IDs únicos y licencias presentes.'

$ErrorActionPreference = 'Stop'
$web = Join-Path $PSScriptRoot '../src/AtlasLetrero.App/wwwroot'
foreach ($kind in @('icons','emojis','fonts')) {
  $items = Get-Content -Raw (Join-Path $web "assets/catalogs/$kind.json") | ConvertFrom-Json
  if (!$items -or ($items.id | Select-Object -Unique).Count -ne $items.Count) { throw "Catálogo inválido: $kind" }
  foreach ($item in $items) { if (!(Test-Path (Join-Path $web "assets/$($item.path)"))) { throw "Asset faltante: $($item.path)" } }
}
foreach ($license in @('bootstrap-icons','tabler-icons','twemoji','Inter','font-5x7','font-5x8','font-6x10','font-8x13','font8x8')) { if (!(Test-Path (Join-Path $web "assets/licenses/$license-LICENSE.txt"))) { throw "Falta licencia $license" } }
$remote = Get-ChildItem $web -Recurse -File -Include *.html,*.css,*.js | Select-String -Pattern 'https?://'
if ($remote) { throw "Referencia remota en runtime: $remote" }
Write-Host 'Assets locales y licencias: PASS'

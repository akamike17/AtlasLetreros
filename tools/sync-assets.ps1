$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../src/AtlasLetrero.App/wwwroot/assets'
$cache = Join-Path $PSScriptRoot '../.cache/assets'
New-Item -ItemType Directory -Force $cache | Out-Null
foreach ($dir in @('icons/bootstrap','icons/tabler','emoji','fonts','catalogs','licenses')) { New-Item -ItemType Directory -Force (Join-Path $root $dir) | Out-Null }
function Fetch($url, $path) { if (!(Test-Path $path)) { Invoke-WebRequest $url -OutFile $path } }
# Explicit upstream whitelist; tags are recorded alongside installed resources.
$banks = @(
  @{name='bootstrap-icons'; tag='v1.11.3'; repo='twbs/icons'; license='LICENSE'; target='icons/bootstrap'; source='icons'},
  @{name='tabler-icons'; tag='v3.1.0'; repo='tabler/tabler-icons'; license='LICENSE'; target='icons/tabler'; source='icons/outline'},
  @{name='twemoji'; tag='v14.0.2'; repo='twitter/twemoji'; license='LICENSE-GRAPHICS'; target='emoji'; source='assets/svg'}
)
$icons = @(); $emojis = @(); $versions = @()
foreach ($bank in $banks) {
  $zip = Join-Path $cache ($bank.name + '.zip')
  Fetch "https://codeload.github.com/$($bank.repo)/zip/refs/tags/$($bank.tag)" $zip
  $expanded = Join-Path $cache $bank.name
  if (!(Test-Path $expanded)) { Expand-Archive -LiteralPath $zip -DestinationPath $expanded }
  $source = (Get-ChildItem $expanded -Directory | Select-Object -First 1).FullName
  $license = Join-Path $source $bank.license
  if (!(Test-Path $license)) { throw "Falta licencia de $($bank.name)" }
  Copy-Item -LiteralPath $license -Destination (Join-Path $root "licenses/$($bank.name)-LICENSE.txt")
  if ($bank.name -eq 'twemoji') {
    $choices = @{ '1f600'='Sonrisa'; '1f4bb'='Computadora portátil'; '2764'='Corazón'; '1f527'='Llave herramienta'; '1f525'='Fuego'; '2705'='Correcto'; '1f680'='Cohete'; '1f389'='Fiesta'; '1f4a1'='Idea luz'; '1f6e0'='Reparación herramientas'; '1f355'='Pizza comida'; '2615'='Café'; '1f31f'='Estrella'; '1f44d'='Pulgar arriba'; '1f3e0'='Casa'; '1f697'='Automóvil' }
    foreach ($id in $choices.Keys) { $relative = "$($bank.target)/$id.svg"; Copy-Item (Join-Path $source "$($bank.source)/$id.svg") (Join-Path $root $relative); $emojis += @{id="emoji-$id"; displayName=$choices[$id]; source=$bank.name; path=$relative; category='Emoji'; tags=@($choices[$id])} }
  } else {
    $choices = if ($bank.name -eq 'bootstrap-icons') { @{ 'pc-display'='Computadora'; 'laptop'='Portátil'; 'tools'='Reparación'; 'shop'='Tienda'; 'clock'='Horario'; 'heart-fill'='Corazón'; 'star-fill'='Estrella'; 'cup-hot'='Café'; 'phone'='Teléfono'; 'wifi'='Internet'; 'printer'='Impresora'; 'envelope'='Correo' } } else { @{ 'device-desktop'='Monitor'; 'cpu'='Procesador'; 'device-mobile'='Celular'; 'bulb'='Luz'; 'home'='Casa'; 'car'='Automóvil'; 'pizza'='Pizza'; 'arrow-right'='Flecha derecha' } }
    foreach ($id in $choices.Keys) { $relative="$($bank.target)/$id.svg"; Copy-Item (Join-Path $source "$($bank.source)/$id.svg") (Join-Path $root $relative); $icons += @{id="$($bank.name)-$id"; displayName=$choices[$id]; source=$bank.name; path=$relative; category= $(if ($id -match 'pc|cpu|device|laptop|phone|wifi|printer') {'Tecnología'} else {'General'}); tags=@($id,$choices[$id],'letrero')} }
  }
  $versions += @{source=$bank.repo; version=$bank.tag; license=$bank.license}
}
# X11 fixed bitmap fonts carry their own public-domain notice in each BDF.
$fonts = @()
foreach ($name in @('5x7','5x8','6x10','8x13')) {
  $file = Join-Path $root "fonts/$name.bdf"
  Fetch "https://raw.githubusercontent.com/olikraus/u8g2/2.35.30/tools/font/bdf/$name.bdf" $file
  $content = Get-Content -Raw $file
  $notice = ($content -split "`n" | Where-Object { $_ -match 'COPYRIGHT|public domain|Public domain' }) -join "`n"
  if ($notice -notmatch '(?i)public domain') { throw "No se comprobó licencia de $name; no se instalará." }
  Set-Content (Join-Path $root "licenses/font-$name-LICENSE.txt") $notice
  $fonts += @{id=$name;displayName="$name clásico";path="fonts/$name.bdf";license='Dominio público (X11)'}
}
Fetch 'https://raw.githubusercontent.com/dhepper/font8x8/master/font8x8_basic.h' (Join-Path $root 'fonts/font8x8_basic.h')
Fetch 'https://raw.githubusercontent.com/dhepper/font8x8/master/font8x8_ext_latin.h' (Join-Path $root 'fonts/font8x8_ext_latin.h')
Set-Content (Join-Path $root 'licenses/font8x8-LICENSE.txt') 'Daniel Hepper font8x8: Public Domain. Source files retain original notice.'
$fonts += @{id='8x8'; displayName='8x8 clásico';path='fonts/font8x8_basic.h';license='Dominio público'}
# Locally distributed UI typeface, OFL.
Fetch 'https://raw.githubusercontent.com/google/fonts/main/ofl/inter/Inter%5Bopsz,wght%5D.ttf' (Join-Path $root 'fonts/Inter.ttf')
Fetch 'https://raw.githubusercontent.com/google/fonts/main/ofl/inter/OFL.txt' (Join-Path $root 'licenses/Inter-LICENSE.txt')
$icons | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $root 'catalogs/icons.json')
$emojis | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $root 'catalogs/emojis.json')
$fonts | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $root 'catalogs/fonts.json')
$versions | ConvertTo-Json | Set-Content (Join-Path $root 'catalogs/versions.json')
Set-Content (Join-Path $root 'licenses/ATTRIBUTION.txt') 'Bootstrap Icons © The Bootstrap Authors, MIT. Tabler Icons © Paweł Kuna, MIT. Twemoji graphics © Twitter, Inc and other contributors, CC-BY 4.0. https://twemoji.twitter.com/ https://creativecommons.org/licenses/by/4.0/ X11 bitmap fonts: individual public-domain notices. font8x8: Daniel Hepper, Public Domain. Inter: Rasmus Andersson, SIL OFL 1.1.'
& (Join-Path $PSScriptRoot 'verify-assets.ps1')


param([string]$OutputRoot = (Join-Path $PSScriptRoot '../src/AtlasLetrero.App/wwwroot/assets'))
$ErrorActionPreference = 'Stop'
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
foreach ($folder in @('icons/bootstrap','emoji/twemoji','fonts','catalogs','licenses')) { New-Item -ItemType Directory -Path (Join-Path $OutputRoot $folder) -Force | Out-Null }
function Fetch-Asset([string]$url,[string]$relative) { Invoke-WebRequest -Uri $url -OutFile (Join-Path $OutputRoot $relative) -TimeoutSec 60 }
$bootstrap = '1.11.3'
$twemoji = 'v14.0.2'
$u8g2 = '9a93ba2e383afb02313c4805b321316aa9b0d2da'
Fetch-Asset "https://raw.githubusercontent.com/twbs/icons/v$bootstrap/LICENSE" 'licenses/bootstrap-icons-LICENSE.txt'
Fetch-Asset "https://raw.githubusercontent.com/twitter/twemoji/$twemoji/LICENSE-GRAPHICS" 'licenses/twemoji-LICENSE-GRAPHICS.txt'
Fetch-Asset "https://raw.githubusercontent.com/twitter/twemoji/$twemoji/LICENSE" 'licenses/twemoji-code-LICENSE.txt'
$icons = @(
 @('pc-display','Computadora','pc computadora monitor reparación tecnología'),
 @('tools','Herramientas','taller reparación herramientas'),
 @('wrench','Llave','taller reparación llave'),
 @('phone','Teléfono','celular móvil teléfono reparación'),
 @('cup-hot','Café','café bebida restaurante'),
 @('house','Casa','casa hogar'),
 @('heart','Corazón','amor corazón salud'),
 @('star','Estrella','estrella destacado'),
 @('arrow-left','Flecha izquierda','flecha dirección izquierda'),
 @('arrow-right','Flecha derecha','flecha dirección derecha'),
 @('wifi','Wi-Fi','internet red tecnología'),
 @('printer','Impresora','impresora copias tecnología'),
 @('shop','Tienda','tienda negocio comercio'),
 @('car-front','Automóvil','auto coche taller'),
 @('bicycle','Bicicleta','bici bicicleta'),
 @('lightning','Rayo','rayo electricidad energía')
)
$catalog = foreach($icon in $icons){$path="icons/bootstrap/$($icon[0]).svg";Fetch-Asset "https://raw.githubusercontent.com/twbs/icons/v$bootstrap/icons/$($icon[0]).svg" $path;@{id=$icon[0];displayName=$icon[1];tags=$icon[2].Split(' ');path=$path;source='bootstrap-icons';category='Iconos';version=$bootstrap}}
ConvertTo-Json -InputObject @($catalog) -Depth 5 | Set-Content (Join-Path $OutputRoot 'catalogs/icons.json') -Encoding utf8
$emojis=@(@('1f600','Sonrisa'),@('1f4bb','Computadora'),@('1f527','Llave'),@('2764','Corazón'),@('1f525','Fuego'),@('2615','Café'),@('1f355','Pizza'),@('1f697','Automóvil'),@('1f31f','Estrella'),@('1f44d','Me gusta'),@('1f389','Celebración'),@('1f4a1','Idea'))
$catalog=foreach($emoji in $emojis){$path="emoji/twemoji/$($emoji[0]).svg";Fetch-Asset "https://raw.githubusercontent.com/twitter/twemoji/$twemoji/assets/svg/$($emoji[0]).svg" $path;@{id=$emoji[0];displayName=$emoji[1];tags=@($emoji[1].ToLower());path=$path;source='twemoji';category='Emojis';version=$twemoji}}
ConvertTo-Json -InputObject @($catalog) -Depth 5 | Set-Content (Join-Path $OutputRoot 'catalogs/emojis.json') -Encoding utf8
$fontCatalog=foreach($font in @('5x7','5x8','6x10','8x13')) {
 Fetch-Asset "https://raw.githubusercontent.com/olikraus/u8g2/$u8g2/tools/font/bdf/$font.bdf" "fonts/$font.bdf"
 $lines=Get-Content (Join-Path $OutputRoot "fonts/$font.bdf")
 $copyright=($lines | Where-Object { $_ -match '^COPYRIGHT ' }) -join "`n"
 if($copyright -notmatch 'Public domain'){throw "Fuente $font fuera de la whitelist de dominio público: $copyright"}
 $copyright | Set-Content (Join-Path $OutputRoot "licenses/font-$font-LICENSE.txt") -Encoding utf8
 @{id=$font;displayName="$font · Fixed";path="fonts/$font.json";source='X11 Fixed vía U8g2';version=$u8g2;license='Public domain'}
}
ConvertTo-Json -InputObject @($fontCatalog) -Depth 5 | Set-Content (Join-Path $OutputRoot 'catalogs/fonts.json') -Encoding utf8
"Twemoji graphics © Twitter and contributors. Licensed under CC-BY 4.0. https://creativecommons.org/licenses/by/4.0/`nSource: https://github.com/twitter/twemoji/tree/$twemoji`nRasterization and resizing performed locally by Atlas Letrero." | Set-Content (Join-Path $OutputRoot 'licenses/ATTRIBUTION.txt') -Encoding utf8
@{bootstrapIcons=$bootstrap;twemoji=$twemoji;u8g2=$u8g2} | ConvertTo-Json | Set-Content (Join-Path $OutputRoot 'catalogs/versions.json') -Encoding utf8
& node (Join-Path $PSScriptRoot 'convert-fonts.mjs') $OutputRoot
if($LASTEXITCODE -ne 0){throw 'Error convirtiendo fuentes'}
& (Join-Path $PSScriptRoot 'verify-assets.ps1')

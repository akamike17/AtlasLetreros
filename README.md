# Atlas Letrero

Aplicación local para diseñar letreros LED con ASP.NET Core 8, HTML, CSS, JavaScript ES modules y Canvas 2D. El servidor sólo escucha en `127.0.0.1:5187`.

## Ejecutar

```powershell
$env:DOTNET_CLI_HOME="$PWD/.cache/dotnet-home"
./.dotnet/dotnet.exe run --project src/AtlasLetrero.App/AtlasLetrero.App.csproj -c Release
```

Abre `http://127.0.0.1:5187/`. Los proyectos se guardan en `Documents/AtlasLetrero/Projects` y el formato `.atlasled` es un ZIP con `manifest.json`.

## Verificación

`tools/smoke.ps1` compila en Release, inicia el servidor, comprueba las cuatro rutas, `/api/health`, catálogos y licencias locales. `tools/sync-assets.ps1` descarga las versiones fijadas de los bancos open source; después de sincronizar la app no necesita Internet para iconos, emojis o fuentes.

El flujo del simulador local compila frames desde el mismo `FrameBuffer` que usa el editor, los entrega al transporte local y verifica checksum antes de mostrar `Correcto`. ESP32 requiere hardware AtlasLED real; sin él, la detección no inventa un dispositivo.

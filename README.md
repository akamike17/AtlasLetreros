# ATLAS LETRERO

Aplicación local para diseñar letreros LED. **Reconstrucción en curso:** consultar `BUILD_PROGRESS.md` para saber qué está verificado y qué falta.

## Abrir en Windows

Haz doble clic en **Abrir Atlas Letrero.cmd**. Compila con el SDK local, inicia el servidor y abre el navegador en `http://127.0.0.1:5088`.

El SDK **.NET 8.0.424** está instalado en `.dotnet/`. No se necesita cambiar el SDK global ni instalar Visual Studio.

Desde PowerShell, en esta carpeta:

```powershell
.\.dotnet\dotnet.exe --version
.\tools\start.ps1
```

También puedes abrir `AtlasLetrero.sln` en Visual Studio 2022, seleccionar `AtlasLetrero.App` como proyecto de inicio y pulsar `F5`. El perfil de Visual Studio usa `http://127.0.0.1:5088` y abre el navegador automáticamente. `global.json` acepta el SDK 8.0.424 o una versión estable posterior instalada por Visual Studio; el lanzador de Windows sigue utilizando el SDK local `.dotnet` 8.0.424.

La primera compilación puede requerir restaurar paquetes NuGet. El diseño utiliza recursos locales, sin CDN.

## Archivos de trabajo

- `ATLAS_LETRERO_FUNCIONAL_MASTER_CORREGIDO.md`: contrato original, conservado.
- `BUILD_PROGRESS.md`: estado y pruebas verificadas.
- `src/AtlasLetrero.App/`: servidor y las cuatro pantallas.
- `tools/sync-assets.ps1`: descarga versiones fijadas de recursos con sus licencias.
- `tools/verify-assets.ps1`: comprueba integridad del catálogo local.

Los proyectos `.atlasled` se guardan en `Documents\AtlasLetrero\Projects`. La recuperación automática utiliza `%LOCALAPPDATA%\AtlasLetrero\Recovery`. El servidor escucha sólo en loopback.

No se ha realizado ningún commit ni push. El SDK local y los archivos de compilación están excluidos de Git.

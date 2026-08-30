# AtlasLetrero

Plataforma local para diseñar, simular, almacenar y reproducir letreros LED comerciales. El host .NET administra el contenido; Atlas Native ejecuta escenas autónomas mediante drivers desacoplados del microcontrolador y del panel.

## Estado V1

La V1 simulada permanece en validación. El editor web ya trabaja con proyectos múltiples durables en IndexedDB, objetos editables, fuentes bitmap determinísticas, imágenes no destructivas con fit/crop/transparencia, canvas lógico dinámico, preview local, easing/transiciones, historial y una salida separada del simulador. El envío usa `SceneDocument`: conserva capas y elementos semánticos de texto, imagen y formas en lugar de reducir todo a un framebuffer. Las escenas se guardan atómicamente en `.runtime/scenes`; el runtime tiene reloj limitado a 30 FPS y conserva explícitamente el estado playing/stopped entre reinicios.

La suite distingue pruebas .NET de pruebas Browser E2E y levanta un proceso ASP.NET real. El prototipo físico, HIL y la certificación de hardware siguen fuera del cierre simulado.

## Inicio rápido

Requisitos: .NET 8 SDK. Node.js sólo es necesario para Browser E2E. MySQL es opcional para las pruebas de infraestructura; la aplicación web actual usa el simulador local.

```powershell
dotnet restore .\AtlasLetreros.slnx
dotnet test .\AtlasLetreros.slnx --configuration Release
dotnet run --project .\AtlasLetreros.csproj
npm install
npm run test:e2e
```

Abrir la dirección local indicada por ASP.NET Core. No guardar contraseñas en `appsettings.json`; usar variables de entorno, secretos de desarrollo o `appsettings.Local.json` ignorado por Git.

## Documentación

- [Arquitectura](docs/architecture.md)
- [Operación, seguridad y energía](docs/operations.md)
- [Agregar drivers y perfiles](docs/extending.md)
- [Compatibilidad y certificación](docs/compatibility.md)
- [Pruebas](docs/testing.md)

La especificación de construcción completa permanece en [AtlasLetrero_Codex_Master.md](AtlasLetrero_Codex_Master.md).

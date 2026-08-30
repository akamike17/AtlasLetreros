# AtlasLetrero

Plataforma local para diseñar, simular, almacenar y reproducir letreros LED comerciales. El host .NET administra el contenido; Atlas Native ejecuta escenas autónomas mediante drivers desacoplados del microcontrolador y del panel.

## Estado V1

El flujo simulado completo está implementado: diseño con texto y animación, protocolo, preview, mapping, envío, persistencia, desconexión del host, reinicio y recuperación píxel por píxel. El firmware portable compila y prueba drivers, API, provisioning, energía, playlists, seguridad, OTA y recovery.

Pendiente para prototipo físico: adaptadores ESP-IDF concretos, composición de `app_main`, build/flash ESP32 o ESP32-S3 y certificación HIL. Nada se declara certificado sólo por compilar o estar soportado por una librería.

## Inicio rápido

Requisitos: .NET 8 SDK. MySQL es opcional para las pruebas de infraestructura; la aplicación web actual usa el simulador local.

```powershell
dotnet restore .\AtlasLetreros.slnx
dotnet test .\AtlasLetreros.slnx --configuration Release
dotnet run --project .\AtlasLetreros.csproj
```

Abrir la dirección local indicada por ASP.NET Core. No guardar contraseñas en `appsettings.json`; usar variables de entorno, secretos de desarrollo o `appsettings.Local.json` ignorado por Git.

## Documentación

- [Arquitectura](docs/architecture.md)
- [Operación, seguridad y energía](docs/operations.md)
- [Agregar drivers y perfiles](docs/extending.md)
- [Compatibilidad y certificación](docs/compatibility.md)
- [Pruebas](docs/testing.md)

La especificación de construcción completa permanece en [AtlasLetrero_Codex_Master.md](AtlasLetrero_Codex_Master.md).

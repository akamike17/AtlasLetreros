# Atlas Letrero — progreso verificable

## Inspección inicial — 2026-09-06

- Carpeta: `C:\Users\Admin\source\repos\AtlasLetras`.
- Único archivo inicial: `ATLAS_LETRERO_FUNCIONAL_MASTER_CORREGIDO.md` (48 685 bytes). Se conserva íntegro.
- No existe implementación, solución, assets ni pruebas que recuperar.
- `git status --short` y `git rev-parse HEAD`: no existe repositorio Git; no hay HEAD inicial.
- Instalados SDK 10.0.301 y 10.0.400; runtime ASP.NET Core/.NET 8.0.30. Falta SDK 8.
- Preparación: descarga oficial del SDK 8.0.424 a TEMP, con verificación SHA512. No reemplaza instalaciones existentes.
- Se aplican las correcciones 0A sobre los dibujos ambiguos: controles de reproducción únicos, simulador permanente y datos de recuperación sólo en pruebas.
- No hacer commit ni push sin orden explícita.

## SDK local y apertura

- Por petición del usuario, SDK 8.0.424 extraído en `.dotnet/` dentro del repo. SHA512 del ZIP contrastado con los metadatos oficiales de Microsoft.
- `.dotnet/dotnet.exe --version`: `8.0.424`.
- Git inicializado en rama local `reconstruccion-atlas`; sin commits ni push.
- Añadidos `Abrir Atlas Letrero.cmd` y `tools/start.ps1`, que usan el SDK local.
- Restore y build Release con SDK local: PASS, 0 advertencias, 0 errores. La caché de paquetes existente se resuelve mediante NUGET_PACKAGES.
- Recursos locales descargados: Bootstrap Icons 1.11.3, Twemoji 14.0.2, fuentes Fixed de dominio público fijadas por commit; `verify-assets.ps1`: PASS.
- Primera implementación de las cuatro pantallas, render compartido, historial, timeline y transporte de simulador escrita. Pendiente aceptación en navegador; NO se declara funcional completo.
- Lanzador probado: servidor iniciado en `http://127.0.0.1:5088`; Editor del proyecto `Prueba` visible en el navegador con Canvas y Simulador. Esta comprobación de apertura NO sustituye la prueba reina.
- Durante el primer arranque, el sandbox impidió crear la carpeta de proyectos en Documentos. Se reinició con permisos aprobados para las rutas de datos; también se eliminó la dependencia del registro de eventos de Windows, conservando logging por consola/archivo del lanzador.
- El build del lanzador tiene 0 errores y 2 advertencias NU1900 por consulta de vulnerabilidades NuGet no disponible. La compilación previa con auditoría de red omitida tuvo 0 advertencias; no confundir ambas ejecuciones.

## Estado

A | EN CURSO | Preparación de herramientas y solución.
B | PENDIENTE | Editor, dibujo, historial, capas y escenas.
C | PENDIENTE | Bancos locales, licencias, texto e imágenes.
D | PENDIENTE | Timeline, efectos, guardado y recuperación.
E | PENDIENTE | Transporte de simulador y verificación.
F | PENDIENTE | Protocolo serial, firmware y pruebas sin hardware.

## Validación

Todavía no hay build, pruebas E2E ni prueba visual ejecutados. No se ha conectado ni identificado hardware.

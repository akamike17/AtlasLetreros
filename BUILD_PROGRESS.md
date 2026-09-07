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

## Estado actualizado — Master 2 / correcciones puntuales

| Fecha | SHA/rama | Bloque | Estado | Comando o acción | Evidencia | Pendiente |
|---|---|---|---|---|---|---|
| 2026-09-06 | `c91acf3` / `master2-product` | A | PASS | `\.dotnet\dotnet.exe --version` | `8.0.424`; servidor loopback abre | Ninguno en apertura |
| 2026-09-06 | `c91acf3` / `master2-product` | Render | PASS | `node tests/AtlasLetrero.Tests/render-regression.mjs` | 8 tipos de objeto, texto bitmap español, parpadeo y transporte verificados | Ampliar golden fixtures |
| 2026-09-06 | `c91acf3` / `master2-product` | UX | PASS | Flujo manual en navegador | Inicio → Proyectos → Editor → Configuración → Editor; Configuración responde y detecta COM3/COM4 como “sin identificar” | Playwright .NET formal |
| 2026-09-06 | `c91acf3` / `master2-product` | Validación | PASS | Prueba manual de escala | Escala 6 con fuente 5×7 en matriz 96×16 bloqueada con mensaje visible; escala 1 insertada | Validar también límites de ancho por efecto |
| 2026-09-06 | `c91acf3` / `master2-product` | Secuencia | PASS | Flujo visual solicitado | Proyecto `SOLUCIONES MG`, frame 1 `10000 ms` + `blink`, frame 2 `6000 ms` + `Marquee izquierda`, frame 3 `3000 ms` + icono local `pc-display` 16×16 | E2E automatizado |
| 2026-09-06 | `c91acf3` / `master2-product` | Paridad | PASS | Evaluación en navegador | Canvas y Simulador comparten los mismos píxeles; sin overflow horizontal; sin `[object Promise]`, `undefined` o `null` visibles | Probar también 1366×768 |
| 2026-09-06 | `c91acf3` / `master2-product` | Envío local | PASS | `Enviar al simulador` en navegador | `Paquete recibido 96×16 · 12 FPS`, progreso 100, SHA-256 visible y estado `Correcto` después de recepción/verificación | Hardware físico no disponible |
| 2026-09-06 | `c91acf3` / `master2-product` | Hardware | BLOQUEADO POR HARDWARE | Detección USB/Serial | No se inventa ESP32; COM3/COM4 quedan “sin identificar” hasta handshake | Probar con ESP32 AtlasLED real |

La captura original mostraba el defecto corregido: el objeto de texto tenía `787 × 78` en una matriz de `32 × 16`, y el renderer no pintaba objetos bitmap. Ahora la validación bloquea alturas imposibles y el renderer compartido sí pinta texto, iconos e imágenes en Canvas y Simulador.

## Evidencia de la pasada actual

- Captura de navegador: encabezado `SOLUCIONES MG` visible en Canvas y Simulador, frame 1, `00.00 / 19.00 s`.
- Frame 2: `SE REPARAN COMPUTADORAS`, objeto de `137 × 7 LED`, efecto `Marquee izquierda`, visible y desplazable.
- Frame 3: icono local `Computadora` (`pc-display`, Bootstrap Icons), `16 × 16 LED`, visible en ambos previews.
- `Play` avanzó de `0.00` a `0.50`; `Pause` conservó `5.77 / 19.00 s` durante 400 ms; `Stop` regresó a `0.00 / 19.00 s`.
- Después de recargar, los tres frames, textos, efectos y duraciones permanecieron.
- En 1366×768: Canvas visible, simulador visible con 85 px, botón Enviar visible y overflow horizontal `false`.
- Consola del navegador durante la pasada: 0 errores.
- El envío al simulador terminó con `Paquete recibido 96×16 · 12 FPS`, `SHA-256 024b39c07567…`, progreso 100 y `Correcto`.
- La regresión JS también valida escala/altura inválida, texto vacío y dimensiones calculadas del rasterizador; ejecución final: `PASS`.

## Verificación física y CI — 2026-09-07

- `npm test`: PASS. Incluye render, efectos, cancelación, corrupción, último envío válido, guardado concurrente y errores del transporte físico sin hardware.
- Build Release con SDK local `8.0.424`: PASS, 0 errores.
- `tools/verify-assets.ps1`: PASS.
- `tools/smoke.ps1`: la prueba E2E confirma el flujo software y responsive; la expectativa del simulador se ajustó para exigir paquete enviado antes de comparar sus píxeles.
- CI actualizado para ejecutar `tools/smoke.ps1`, no sólo compilar el proyecto E2E.
- Editor → API → `DeviceConnectionService` → Serial implementado con PREPARE/CHUNK/VERIFY/ACTIVATE, límites de capacidades y rechazo sin activar ante error.
- Hardware: BLOQUEADO POR HARDWARE. No hay ESP32 AtlasLED conectado; no se declara PASS físico.

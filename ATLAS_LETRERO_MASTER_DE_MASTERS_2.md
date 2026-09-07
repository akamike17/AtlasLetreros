# ATLAS LETRERO — MASTER DE MASTERS 2

## Contrato maestro de producto, UX, render, simulador, mapping, despliegue, firmware y aceptación

**Versión:** 2.0  
**Base protegida:** `akamike17/AtlasLetreros`, rama `reconstruccion-atlas`, commit `c91acf3b03c6edfd2d97054438cb686d71ab4057`.

Este documento sustituye cualquier interpretación improvisada del agente. No autoriza reescribir por costumbre. Se conserva toda pieza de `c91acf3` que tenga evidencia de funcionamiento y se corrige únicamente lo que incumpla el contrato, duplique estado/render, produzca UX falsa o falle pruebas reales.

Flujo final obligatorio:

```text
CREAR → DISEÑAR → ANIMAR → SIMULAR → GUARDAR → REABRIR
→ CONFIGURAR DESTINO → ENVIAR → VERIFICAR → REPRODUCIR
```

La aplicación debe sentirse como un editor real de letreros LED, no como dashboard de diagnóstico.

---

# 0. REGLA CERO — NO PERDER OTRA VEZ EL DESARROLLO

Antes de modificar: `git status`, `git branch --show-current`, `git rev-parse HEAD`, verificar SHA, crear rama nueva desde `c91acf3`.

**Prohibido:** `git reset --hard`, `git clean -fd`, `git clean -fdx`, borrar carpetas para volver a clonar, sobrescribir copias, force push o tocar `main`. No eliminar `reconstruccion-atlas`. No commit/push sin orden explícita del usuario.

Rama sugerida: `master2-product`.

---

# 1. QUÉ SE CRUZÓ PARA ESTE MASTER 2

Se cruzaron: el MASTER anterior, el código real de `c91acf3`, la auditoría del commit y patrones de WLED, ESPHome, xLights y Jinx!.

De **WLED** se toma como referencia conceptual la separación entre configuración física y contenido, matrices 2D, efectos configurables, presets/segmentos y UI orientada al usuario. No se copia ni se depende de WLED.

De **ESPHome** se toma la separación matriz lógica/mapeo físico, `width`, `height`, rotación, `pixel_mapper`, serpentina, gamma y efectos parametrizados. El usuario siempre diseña en coordenadas lógicas; el mapper resuelve el cableado.

De **xLights** se toma el concepto de modelo lógico independiente del controlador, Render Buffer, esquina de inicio, zigzag, capas, presets, secuenciación y preview. Atlas Letrero debe ser mucho más simple.

De **Jinx!** se toma matrix patch, selección explícita de dispositivo, preview visible y salida sólo después de configurar destino.

Son patrones, no dependencias.

---

# 2. DIAGNÓSTICO DE `c91acf3`

**Conservar:** .NET 8, ASP.NET Core, HTML estático, JS Modules, Canvas 2D, `FrameBuffer`, `renderScene`, escenas, frames, capas, objetos, Undo/Redo, fuentes bitmap, iconos, Twemoji local, `.atlasled`, escritura atómica, recovery, `SimulatorTransport`, SHA-256, protocolo/CRC y descubrimiento Serial.

**No cuenta como terminado:** que compile, que abra el editor, que exista un botón/end-point, que un COM aparezca, que exista `blink`, que un checksum se calcule o que `BUILD_PROGRESS.md` diga PASS.

**Deuda obligatoria:** prueba reina E2E ausente, editor monolítico, simulador visualmente pequeño, timeline invasivo, jerarquía visual débil, firmware no demostrado, estado de progreso contradictorio, mapping físico incompleto, ausencia de pruebas de fallo de transferencia y falta de aceptación 1920×1080/1366×768.

---

# 3. LAS ÚNICAS CUATRO PANTALLAS

1. **Inicio:** ATLAS LETRERO, Nuevo letrero, Proyectos, recientes, Configurar dispositivo, estado físico discreto.
2. **Proyectos:** crear, abrir, duplicar, renombrar, eliminar con confirmación, buscar, ordenar, importar/exportar `.atlasled`.
3. **Editor:** toolbar, herramientas, Canvas grande, simulador permanente, inspector, capas, destino, timeline, estado guardar/enviar.
4. **Configuración / Dispositivo:** matriz lógica/física, mapping, brillo/gamma/color order, Serial/USB, test de matriz, handshake y capacidades.

No crear navegación genérica tipo `Nuevo | Biblioteca | Dispositivos | Reproducción | Ajustes`.

---

# 4. EDITOR — JERARQUÍA VISUAL OBLIGATORIA

Prioridad visual:

```text
CANVAS > SIMULADOR > HERRAMIENTAS > INSPECTOR > CAPAS > TIMELINE > DESTINO
```

Canvas debe ocupar la mayor área. El simulador será permanente y mínimo ~160 px de alto en 1080p; en 1366×768 puede bajar, pero nunca desaparecer. No scroll horizontal, no doble toolbar, no playback duplicado, no labels internos, no paneles vacíos enormes.

Layout objetivo:

```text
┌ Proyecto | Guardar | Undo | Redo | Escena | Estado ┐
├ Tools ┬──────────── CANVAS ────────────┬ Simulador ┤
│       │                                ├ Inspector ┤
│       │                                ├ Destino   ┤
├───────┴────────────────────────────────┴───────────┤
│ Capas compactas                                   │
├───────────────────────────────────────────────────┤
│ Timeline compacto + Play/Pause/Stop               │
└───────────────────────────────────────────────────┘
```

---

# 5. CANVAS Y HERRAMIENTAS

Herramientas V1: Selección, Lápiz, Borrador, Línea, Rectángulo, Elipse, Relleno, Texto, Icono, Emoji, Imagen.

Debe soportar zoom, pan, grid, pixel-perfect, selección, drag, resize, rotación, teclado, Delete, copiar/pegar, Undo/Redo. Cada herramienta cambia cursor, muestra estado activo, tooltip y nombre en español. No cambiar silenciosamente de modo.

---

# 6. UNA SOLA FUENTE DE RENDER

Arquitectura obligatoria:

```text
Project → Scene → Frame → Layers → Objects
→ SceneRenderer → Logical FrameBuffer
   ├→ CanvasView
   ├→ SimulatorView
   └→ PixelMapper → PhysicalFrame → Transport
```

Prohibido crear motores semánticos distintos para Canvas, simulador y ESP32.

Para el mismo `project + scene + time`, Canvas y simulador deben partir del mismo framebuffer lógico y producir el mismo hash lógico.

---

# 7. MATRIZ LÓGICA VS MATRIZ FÍSICA

La matriz lógica contiene sólo lo que diseña el usuario: `width`, `height`.

La configuración física contiene: tipo, ancho/alto, origen, eje principal, serpentina, rotación, flipX/flipY, orden de color, gamma, límite de brillo, filas/columnas de panel.

Orígenes: TopLeft, TopRight, BottomLeft, BottomRight.  
Dirección: Rows / Columns.  
Wiring: Progressive / Serpentine.

Orden documentado:

```text
logical x,y → rotation → flips → axis/origin → serpentine → physical index
```

El renderer nunca debe conocer el cableado.

---

# 8. HARDWARE ABSTRAÍDO

V1: `AddressableMatrix` para WS2812B/WS2811/SK6812/NeoPixel compatibles.

Preparar modelo para `Hub75Matrix` (P10/P5/HUB75) sin obligar implementación completa en primer cierre. El dominio no debe asumir que todo píxel físico es WS2812.

Firmware debe usar interfaz tipo:

```text
IDisplayDriver
  Initialize(config)
  Show(frame)
  Clear()
  Capabilities()
```

---

# 9. TEXTO, ICONOS, EMOJIS E IMÁGENES

**Texto:** objeto real con texto, fuente bitmap, color, X/Y, escala, spacing, efecto, velocidad, inicio/duración, visible/opacidad. Fuentes mínimas 5×7, 5×8, 6×10, 8×13. Texto largo puede salir del canvas y usar marquee; no truncar silenciosamente.

**Iconos:** banco local con PC, herramientas, corazón, estrella, casa, teléfono, impresora, wifi, carro, bicicleta, tienda, rayo, café. Convertir a representación determinista para LED; firmware no depende del SVG.

**Emojis:** banco local, búsqueda, preview, bitmap determinista.

**Imágenes:** PNG/JPG/WEBP, resize nearest-neighbor, fit/crop, transparencia, reducción a matriz, preview. El proyecto debe ser portable y no depender de una ruta temporal externa.

---

# 10. CAPAS, ESCENAS Y TIMELINE

Cada frame tiene capas; cada capa: `id,name,visible,locked,opacity,objects[]`. Render bottom→top. Acciones: crear, renombrar, duplicar, eliminar, bloquear, ocultar, mover.

Proyecto puede tener múltiples escenas, cada una con frames, duración y loop. Nunca dejar proyecto sin escena/capa válida.

Timeline: frame select/create/duplicate/delete/reorder, duración visible, Play/Pause/Stop, inicio/anterior/siguiente/final, loop y tiempo actual. El usuario no debe calcular frames para lograr “parpadea 10 segundos”.

---

# 11. MOTOR DE EFECTOS V2

Modelo sugerido:

```json
{
  "type": "blink",
  "enabled": true,
  "startMs": 0,
  "durationMs": 10000,
  "speed": 1,
  "parameters": {}
}
```

Mínimos: Ninguno, Parpadeo, izquierda, derecha, slide/entrada, Fade in/out, Aparecer, Desaparecer, Pulso, Wipe, Color wipe, Twinkle, Scan.

`blink` debe definir `frequencyHz`, `dutyCycle`, `phaseMs`, `startMs`, `durationMs`. Default 1 Hz, 50 %, fase 0.

Agregar presets simples de efectos: Parpadeo lento, Marquee rápido, Pulso suave.

---

# 12. SIMULADOR — DOS MODOS REALES

**Vista en vivo:** `SceneRenderer → Logical FrameBuffer`.

**Paquete recibido:** al pulsar Enviar: snapshot → compilar → serializar → hash → transport → recepción → validación hash → carga → reproducción del paquete recibido.

UI debe indicar `Vista en vivo` o `Paquete recibido · SHA-256 …`.

Prohibido mostrar `Correcto` antes de recepción, verificación y activación. No pintar la vista en vivo y llamarla paquete recibido.

---

# 13. PIXEL MAPPER

Módulo único:

```text
map(x,y,config) → physicalIndex
mapFrame(logicalFrame,config) → physicalFrame
```

Tests: 4×4 progressive, serpentine rows/columns, 4 esquinas, 4 rotaciones, flips y combinaciones principales. Ningún índice fuera de rango y cada píxel físico exactamente una vez.

Propiedad:

```text
unique(map(all logical pixels)) == width*height
```

Pantalla Configuración debe mostrar un preview numerado del wiring para que el usuario vea el orden físico antes de enviar.

---

# 14. BRILLO, GAMMA Y ORDEN DE COLOR

Separar opacidad objeto, opacidad capa, brillo de proyecto, límite de hardware y gamma. No aplicar gamma dos veces.

Salida física sugerida:

```text
logical RGBA → global brightness → gamma → color order
```

Orden de color configurable: RGB, RBG, GRB, GBR, BRG, BGR. Incluir patrón de diagnóstico Rojo/Verde/Azul/Blanco/Negro.

---

# 15. DESCUBRIMIENTO Y HANDSHAKE

Un COM no es un ESP32.

Flujo:

```text
enumerar puerto → usuario elige → abrir → HELLO
→ validar magic/protocol → leer DeviceId/Firmware/Capabilities
→ sólo entonces "ESP32 AtlasLED conectado"
```

Si sólo existe el puerto: `Puerto COMx detectado · dispositivo no identificado`.

Handshake mínimo: HELLO, HELLO_ACK, CAPABILITIES, PING/PONG, PREPARE, CHUNK/FRAME, VERIFY, ACTIVATE, ABORT, STATUS.

Capabilities: protocolVersion, deviceId, firmwareVersion, hardwareFamily, maxWidth, maxHeight, maxPixels, maxFps, supportedColorOrders, supportsStorage, freeHeap.

---

# 16. PIPELINE DE ENVÍO

```text
Validate
→ immutable snapshot
→ compile logical timeline
→ physical mapping
→ encode
→ checksum
→ connect
→ capabilities
→ compatibility check
→ prepare
→ upload chunks
→ device verify
→ host verify
→ activate
→ confirm active checksum
→ success
```

Comprobar dimensiones/FPS/píxeles contra capacidades antes de enviar.

UI muestra fases reales: Validando, Compilando, Mapeando, Conectando, Preparando, Enviando %, Verificando, Activando, Correcto.

---

# 17. LAST KNOWN GOOD Y FALLOS

Dispositivo: `ActivePackage`, `CandidatePackage`, `LastKnownGood`.

Nunca reemplazar activo hasta upload completo + checksum + parse + memoria válidos. Si falla: Candidate descartado, Active intacto.

Inyectar fallo en Connect, Prepare, chunk inicial/intermedio/final, Verify, Activate y ACK final. Nunca marcar Correcto; liberar conexión; permitir reintentar; preservar LastKnownGood.

---

# 18. FIRMWARE V1

Crear `firmware/AtlasLed/`.

Objetivos: ESP32 DevKit/ESP32-S3, driver addressable V1, Serial protocol, watchdog, safe boot, LastKnownGood, capability report, configuración runtime de width/height/mapping/color order/brightness/fps.

No hardcodear 32×16 como única matriz. No exigir recompilar firmware por cada letrero si la configuración puede viajar en protocolo.

---

# 19. MODO SIN HARDWARE ES OBLIGATORIO

Sin ESP32 el usuario debe completar: crear, diseñar, animar, simular, guardar, reabrir, enviar a simulador local y verificar checksum. Ese gate puede declarar **software/simulador listo**.

Nunca declarar **hardware probado** sin evidencia física.

---

# 20. PERSISTENCIA `.atlasled`

Mantener contenedor ZIP. Debe ser portable; si hay assets propios, incluirlos dentro.

Guardado: temp → flush → read-back validation → atomic replace. Autosave separado y recovery sólo si es más nuevo. No borrar recovery antes de guardado correcto.

Si cambia el formato, incrementar `formatVersion` y crear migrador. Nunca romper proyectos V1 silenciosamente.

---

# 21. SEGURIDAD Y LÍMITES

Loopback por default; limitar payloads; validar nombres; evitar path traversal; límites ZIP/JSON/imágenes; timeouts/cancellation; no ejecutar contenido importado.

Definir max matrix software, object count, scenes, frames, package size, image bytes, chunk size y duración de render. Hardware puede reportar límites menores.

---

# 22. RENDIMIENTO

Metas: edición fluida 32×16, 64×32 y 128×64. No reinstanciar renderer por tick, no serializar proyecto completo por mousemove, no recrear todo el DOM sin necesidad.

Benchmark mínimo con texto + icono + drawing + 3 capas + 2 efectos a 30fps en esas tres resoluciones.

---

# 23. MODULARIZAR EL FRONTEND SIN REESCRIBIR

Dividir el actual `editor-app.js` hacia responsabilidades:

```text
editor-app.js
editor-shell.js
toolbar-controller.js
tools-controller.js
canvas-controller.js
simulator-controller.js
inspector-controller.js
layers-controller.js
timeline-controller.js
deployment-controller.js
keyboard-controller.js
scene-renderer.js
framebuffer.js
pixel-mapper.js
```

Mantener un solo `EditorState`; no introducir stores duplicados.

---

# 24. UNDO/REDO Y TECLADO

Undo/Redo cubre dibujo, borrar, insertar, mover, resize, propiedades, capas, frames, escenas y efectos. No incluye play/pause, zoom/pan o conexión.

Atajos: Ctrl+S, Ctrl+Z, Ctrl+Y, Delete, Esc, Space (si no hay input), Ctrl+C/V, flechas 1px, Shift+flechas 5px. No secuestrar teclas dentro de inputs.

---

# 25. MENSAJES VERDADEROS

Correctos: `Guardado`, `Sin guardar`, `Guardando…`, `Puerto detectado`, `Dispositivo AtlasLED identificado`, `Paquete recibido y verificado`, `Checksum no coincide`.

Prohibido `Correcto`, `Listo`, `Conectado` sin evidencia/contexto.

---

# 26. PRUEBAS UNITARIAS/JS/GOLDEN

.NET: ProjectPackageService, AtomicFileWriter, PacketCodec, CRC32, handshake parser, validation, migration, PixelMapper.

JS: FrameBuffer, SceneRenderer, effects, duration/frameAt, PixelMapper, hash, package decode, history, project model.

Golden fixtures: texto 32×16, icono PC 32×16, corazón 16×16, blink 32×16 y mapper 4×4. Comparar hashes, no sólo screenshots.

---

# 27. E2E REAL CON PLAYWRIGHT

Browser real. Fallar por `console.error`, `pageerror`, requestfailed o HTTP 4xx/5xx inesperado.

No vale una prueba que invoque sólo servicios. Debe tocar la UI visible.

---

# 28. PRUEBA REINA — MASTER 2

Caso:

```text
Proyecto: MG SOLUTION
Matriz: 32×16
Texto: SE REPARAN COMPUTADORAS
Icono: PC
Efecto: Parpadeo
Frecuencia: 1 Hz
Duración: 10 s
Destino: Simulador local
```

Flujo: Inicio → Nuevo → crear → Editor → nombre → insertar texto → fuente bitmap → icono PC → blink 10s → Play/Pause/Stop → verificar simulador → Guardar → recargar → abrir → verificar persistencia → Enviar simulador → checksum → paquete recibido → reproducir paquete → comparar paridad → Correcto.

Assertions: cero errores JS/HTTP, texto editable, icono real del catálogo, nada hardcodeado, duración ~10000ms, Canvas y simulador con contenido, save/reload conserva, receivedHash==expectedHash, paquete reproduce y paridad lógica coincide.

---

# 29. R2–R7

**R2:** 16×16, dibujar corazón continuo como un solo DrawingObject, mover/blink/undo/redo/save/open/simulator/deploy.

**R3 usuario hostil:** nombres largos, dimensiones/FPS inválidos, duración negativa, imagen enorme, ZIP/manifest corrupto, borrar última escena/capa, doble click Enviar, cancelar 50 %, desconectar puerto.

**R4 transferencia:** paquete A activo, B falla; A sigue activo, B no activa, UI no dice Correcto.

**R5 equivalencia:** hash Editor source == live simulator == compiled logical frame.

**R6 mapping:** patrón A…P sobre 4×4 para cada origen/serpentina/rotación.

**R7 hardware:** handshake, capabilities, patrón RGB, envío, active checksum, reboot y LastKnownGood. Marcar `BLOQUEADO POR HARDWARE` si no hay dispositivo.

---

# 30. RESPONSIVE Y ACEPTACIÓN VISUAL

Probar 1920×1080, 1600×900 y 1366×768. Sin overflow horizontal; Canvas usable; simulador visible; botón Enviar visible; playback accesible; timeline no tapa Canvas; inspector usable.

Visualmente: marca correcta, español, Canvas protagonista, simulador suficientemente grande, herramientas entendibles, sin look de telemetría, sin panel muerto, sin duplicación y estado físico separado del simulador.

---

# 31. BUILD_PROGRESS.md VERIFICABLE

Rehacerlo con columnas: fecha, SHA, bloque, estado, comando ejecutado, resultado, evidencia, pendiente.

Estados permitidos: NO INICIADO, EN PROGRESO, PASS, FAIL, BLOQUEADO POR HARDWARE.

Prohibido PASS sin comando/evidencia.

---

# 32. BLOQUES DE IMPLEMENTACIÓN

**A Baseline:** rama, pruebas actuales, inventario, corregir BUILD_PROGRESS.  
**B UX Editor:** modularización, jerarquía, Canvas/simulador, responsive.  
**C Modelo físico/Mapper:** config física, preview wiring, gamma/color order, tests.  
**D Timeline/Efectos:** duración real, blink 10s, presets, efectos mínimos.  
**E Simulador verificable:** live vs received, package, checksum, cancel/failure.  
**F Persistencia portable:** assets, validation, migration, import/export, recovery.  
**G Serial/Firmware:** handshake, capabilities, firmware, LastKnownGood, failures.  
**H E2E/Reina:** Playwright, R1–R6, responsive, console/network gates.

Después de cada bloque ejecutar su gate antes de seguir.

---

# 33. DEFINITION OF DONE

Sólo terminado cuando:

```text
[ ] Release build PASS
[ ] pruebas .NET PASS
[ ] pruebas JS PASS
[ ] Golden PASS
[ ] PixelMapper properties PASS
[ ] R1 PASS
[ ] R2 PASS
[ ] R3 PASS
[ ] R4 simulator PASS
[ ] R5 PASS
[ ] R6 PASS
[ ] 1920/1600/1366 PASS
[ ] zero console.error/pageerror
[ ] zero HTTP inesperado
[ ] save/reload PASS
[ ] checksum PASS
[ ] cancel/recovery PASS
[ ] README honesto
[ ] BUILD_PROGRESS honesto
[ ] no TODO/Fake/Stub en ruta V1
[ ] no botón muerto
[ ] no éxito falso
[ ] no demo hardcodeada
[ ] no renderer duplicado
[ ] main intacto
```

R7 hardware puede quedar BLOQUEADO POR HARDWARE. Eso permite declarar software/simulador listo, no hardware probado.

---

# 34. INSTRUCCIÓN DIRECTA PARA CODEX / AGENTE

Leer completo antes de escribir. Inspeccionar `c91acf3`, confirmar SHA/rama, crear rama no destructiva, ejecutar baseline, corregir progreso, implementar A→H, ejecutar gates, abrir navegador y realizar flujo real.

No preguntar permiso entre correcciones normales. Detenerse sólo por hardware requerido, operación destructiva, credencial o ambigüedad que arriesgue datos.

No commit/push sin orden explícita.

---

# 35. CRITERIO FINAL

La pregunta no es “¿compila?” ni “¿tiene muchas pruebas?”.

La pregunta final es:

> **¿Una persona puede crear un letrero, verlo exactamente como quedará, guardarlo, abrirlo después, enviarlo y comprobar que el destino recibió exactamente lo que diseñó?**

Si no existe un **sí demostrable**, Atlas Letrero no está terminado.

---

# 36. FUENTES TÉCNICAS CRUZADAS

- Implementación real `akamike17/AtlasLetreros@c91acf3`.
- WLED: matrices 2D, efectos, segmentos/presets y configuración de dispositivo.
- ESPHome Addressable Light Display: ancho/alto, rotation y pixel_mapper.
- ESPHome Light Effects: pulse, wipe, scan, twinkle y efectos parametrizados.
- xLights: Matrix Model, starting corner, zigzag, Render Buffer, capas y presets.
- Jinx! LED Matrix Control: patch de matriz, output devices, preview y efectos.

Se usan como patrones de ingeniería; Atlas Letrero conserva identidad propia, offline-first y alcance más simple.


# ATLAS LETRERO — CONTRATO MAESTRO DE IMPLEMENTACIÓN FUNCIONAL
## Versión: reconstrucción desde cero, UI primero, flujo completo, sin scaffolds falsos

> **Este documento manda sobre cualquier inferencia del agente.**
>
> El directorio puede partir vacío. Si existen archivos creados por intentos anteriores, no asumir que están bien: conservar únicamente lo que demuestre cumplir este contrato.
>
> **Objetivo final:** una aplicación local para Windows 10 que permita crear un letrero LED de forma visual, automática y comprensible:
>
> **CREAR → DISEÑAR → ANIMAR → SIMULAR → GUARDAR → ELEGIR DESTINO → ENVIAR**
>
> Máximo **4 pantallas**. El Editor concentra casi todo.
>
> No detenerse a pedir permiso entre bloques. Trabajar hasta completar el producto o encontrar un bloqueo real que no pueda resolverse localmente.

---


# 0A. CORRECCIONES POSTERIORES — APLICAR ANTES DE PROGRAMAR

> **Estas correcciones tienen prioridad sobre cualquier interpretación ambigua del resto del documento.**
>
> No sustituyen el contrato maestro: lo afinan para reconstruir la versión funcional que se perdió sin volver a caer en la interfaz genérica.

## 0A.1 REVISAR ANTES DE ESCRIBIR

Antes de modificar código:

1. inspeccionar la solución completa existente;
2. identificar qué partes YA cumplen este contrato;
3. conservar lo que esté probado y funcional;
4. comparar UI, estado, render, simulador, timeline, assets y persistencia contra este documento;
5. anotar en `BUILD_PROGRESS.md` qué se conserva, qué se corrige y qué falta;
6. sólo después comenzar a editar.

**Prohibido empezar reescribiendo la aplicación por costumbre.**

Si existe una implementación funcional parcial, se corrige sobre ella.
Sólo se reemplaza una pieza cuando se demuestre que incumple el contrato o bloquea el flujo real.

## 0A.2 PROTECCIÓN DEL DESARROLLO

Antes de la primera modificación:

- registrar `git status`;
- registrar `git rev-parse HEAD` si existe repositorio;
- crear una rama/copia de trabajo no destructiva si Git está disponible;
- no ejecutar `git reset --hard`;
- no ejecutar `git clean -fd`;
- no borrar carpetas para "empezar limpio";
- no sobrescribir una versión existente sin copia;
- no hacer commit/push salvo orden explícita del usuario.

La reconstrucción debe ser **reversible**.

## 0A.3 NO VOLVER A LA INTERFAZ GENÉRICA

La aplicación final NO debe convertirse en una navegación genérica tipo:

```text
Nuevo | Proyectos | Biblioteca | Dispositivos | Reproducción | Ajustes
```

Eso contradice el objetivo de **máximo 4 pantallas** y el principio de que **el Editor concentra casi todo**.

Las únicas pantallas siguen siendo:

1. Inicio
2. Proyectos
3. Editor
4. Configuración / Dispositivo

Biblioteca, reproducción, assets, escenas, timeline y simulador se resuelven dentro del flujo correspondiente, principalmente dentro del Editor.

## 0A.4 SIMULADOR — ACLARACIÓN DEFINITIVA

El simulador:

- está visible permanentemente dentro del Editor;
- no se sustituye por un modal;
- no se sustituye por una página aparte;
- no se sustituye por texto;
- no depende de un ESP32 físico;
- utiliza el mismo `SceneRenderer` y `FrameBuffer` que el Canvas;
- reproduce exactamente el estado actual del proyecto;
- al enviar al `Simulador local`, recibe el paquete compilado, lo carga y verifica checksum/hash antes de mostrar `Correcto`.

Un modal puede existir únicamente como **vista ampliada opcional**, nunca como reemplazo del simulador permanente.

## 0A.5 PRUEBA VISUAL DE RECUPERACIÓN — NO HARDCODEAR

Agregar una prueba E2E/aceptación que reproduzca exactamente este caso de usuario:

```text
Proyecto: MG SOLUTION
Texto: SE REPARAN COMPUTADORAS
Icono: computadora / PC
Efecto: Parpadeo
Duración de referencia: 10 segundos
Destino: Simulador local
```

Reglas:

- `MG SOLUTION` y `SE REPARAN COMPUTADORAS` son **datos de prueba**, no contenido hardcodeado del producto;
- el texto debe seguir siendo editable;
- el icono PC debe venir del banco local de iconos;
- el parpadeo debe ser un efecto real parametrizable;
- los 10 segundos deben pertenecer a la escena/efecto de la prueba, no quedar fijados globalmente;
- Canvas y Simulator deben mostrar el mismo resultado;
- Guardar → cerrar/recargar → abrir debe conservarlo;
- Enviar al simulador debe terminar en `Correcto` sólo después de verificar el paquete.

Esta prueba existe para demostrar que la interfaz funcional anterior puede reconstruirse mediante funciones reales, no mediante una demo fija.

## 0A.6 CORRECCIONES VISUALES Y DE UX

- Marca visible: `ATLAS LETRERO`.
- Todos los controles de usuario en español.
- No mostrar nombres internos como `select`, `pencil`, `rect`, `fill`.
- No duplicar toolbars o controles de reproducción.
- Canvas debe conservar el mayor espacio útil.
- Simulator + Inspector + Destino deben coexistir en el lateral sin expulsar el Canvas.
- Timeline compacto y visible.
- No scroll horizontal en 1920×1080.
- A 1366×768 se compacta el lateral, pero el simulador permanece visible.
- Estado `Sin dispositivo` significa hardware físico no conectado; no invalida el Simulador local.
- Nunca mostrar un dispositivo real si sólo existe Internet o un puerto COM genérico.

## 0A.7 REVISIÓN FUNCIONAL ANTES DE DECLARAR ÉXITO

Además de build/tests, el agente debe abrir el navegador y ejecutar manualmente:

```text
crear proyecto
→ abrir Editor
→ insertar MG SOLUTION
→ insertar SE REPARAN COMPUTADORAS
→ insertar icono PC
→ configurar parpadeo 10 s
→ Play/Pause/Stop
→ comprobar Simulator
→ Guardar
→ recargar/abrir
→ Enviar a Simulador local
→ verificar checksum
→ confirmar estado Correcto
```

Durante esa ejecución debe comprobar:

- cero `console.error`;
- cero `pageerror`;
- cero 404/500 inesperados;
- cero `[object Promise]`;
- cero `undefined/null` visibles;
- Undo/Redo funciona;
- el simulador no pierde sincronía;
- el estado mostrado coincide con lo ocurrido realmente.

## 0A.8 REGLA PARA CODEX / AGENTE

Si la implementación existente difiere de este contrato:

1. **no inventar otra arquitectura visual;**
2. mostrar primero qué encontró;
3. conservar todo lo compatible;
4. corregir por bloques verticales;
5. ejecutar el flujo real después de cada bloque;
6. si algo falla, corregir el producto y repetir la prueba;
7. no declarar terminado mientras la prueba visual de recuperación no pase.


---

# 0. POR QUÉ LOS INTENTOS ANTERIORES FALLARON

Antes de programar, asumir que estos son defectos conocidos y prohibidos:

1. Construir botones sin comportamiento.
2. Construir servicios/endpoints y declarar la fase terminada sin usarlos desde UI.
3. Sustituir el simulador real por un texto tipo “Simulador disponible”.
4. Mostrar “Correcto” al enviar sin comprobar que el destino recibió/renderizó el contenido.
5. Filtrar nombres internos al usuario: `select`, `pencil`, `rect`, `fill`, etc.
6. Dejar inspector, capas o timeline como placeholders.
7. Crear demos hardcodeadas (`MG SOLUTIONS`, `HOLA`, etc.) para aparentar funcionalidad.
8. Confundir build verde con aplicación funcional.
9. No abrir realmente las 4 pantallas en navegador.
10. Renderizar una Promise y terminar mostrando `[object Promise]`.
11. No revisar consola JavaScript ni respuestas HTTP 4xx/5xx.
12. Crear demasiadas fases y resolver cada una con el mínimo literal.
13. Omitir campos pedidos porque “todavía no se usan”.
14. No descargar los bancos open source solicitados.
15. Obligar al usuario a construir letras/iconos píxel por píxel.
16. Confundir Internet disponible con ESP32 conectado.
17. Crear estados o conexiones simuladas que parecen hardware real.
18. Duplicar motores de render: uno para editor y otro distinto para simulador.
19. Dejar UI horizontalmente rota o con scroll lateral innecesario.
20. Hacer pruebas técnicas que no reproducen el flujo de un usuario real.

**Regla correctiva:** ninguna funcionalidad cuenta hasta que puede ejecutarse desde la interfaz y produce un resultado visible verificable.

---

# 1. PRINCIPIOS DE DISEÑO

La interfaz sigue una relación directa entre tarea, control y resultado:

- controles relacionados deben estar juntos;
- información crítica debe verse simultáneamente;
- la pantalla debe seguir el orden natural del trabajo;
- evitar horizontal scroll;
- una misma acción debe verse y comportarse igual en toda la aplicación;
- acciones destructivas requieren confirmación;
- los estados deben expresarse directamente, sin exigir que el usuario “deduzca” qué ocurrió.

Aplicado a Atlas Letrero:

```text
HERRAMIENTA → CANVAS → OBJETO/CAPA → TIMELINE → SIMULADOR → DESTINO → RESULTADO
```

El usuario no debe cambiar de pantalla para ver si un diseño funciona.

---

# 2. STACK OBLIGATORIO

## Runtime

```text
.NET 8
ASP.NET Core 8
Controllers / Minimal API exclusivamente para JSON y archivos
Static Files
System.Text.Json
System.IO.Compression
DI y Logging nativos
```

## Frontend

```text
HTML5
CSS3
JavaScript ES Modules
Canvas 2D
Fetch API
File API
Drag & Drop API
Pointer Events
ResizeObserver
```

## PROHIBIDO

```text
Razor
Razor Pages
WinForms
Blazor
Vue
Angular
React
Svelte
SPA framework
CDN
jQuery como base
nube obligatoria
Docker obligatorio
Redis
login
usuarios
roles
API keys
mutation testing
Stryker
```

## NuGet permitido cuando aporta valor

Preferir pocos paquetes:

```text
System.IO.Ports
SixLabors.ImageSharp           # procesamiento de imágenes en backend si se necesita
Microsoft.Playwright           # SOLO proyecto de pruebas E2E, nunca runtime
```

No añadir una dependencia si JavaScript/.NET nativo ya cubre bien la necesidad.

Crear `global.json` para fijar SDK .NET 8 compatible y evitar divergencia local/CI.

---

# 3. ESTRUCTURA EXACTA DEL PROYECTO

```text
AtlasLetrero/
│
├─ AtlasLetrero.sln
├─ global.json
├─ README.md
├─ BUILD_PROGRESS.md
│
├─ src/
│  └─ AtlasLetrero.App/
│     ├─ AtlasLetrero.App.csproj
│     ├─ Program.cs
│     │
│     ├─ Controllers/
│     │  ├─ HealthController.cs
│     │  ├─ ProjectsController.cs
│     │  ├─ AssetsController.cs
│     │  └─ DevicesController.cs
│     │
│     ├─ Models/
│     │  ├─ ProjectDocument.cs
│     │  ├─ MatrixConfiguration.cs
│     │  ├─ SceneDocument.cs
│     │  ├─ LayerDocument.cs
│     │  ├─ ObjectDocument.cs
│     │  ├─ FrameDocument.cs
│     │  ├─ EffectDocument.cs
│     │  ├─ AssetCatalogItem.cs
│     │  ├─ DeviceCapabilities.cs
│     │  └─ DeviceStatus.cs
│     │
│     ├─ Services/
│     │  ├─ AppPaths.cs
│     │  ├─ AtomicFileWriter.cs
│     │  ├─ ProjectPackageService.cs
│     │  ├─ ProjectIndexService.cs
│     │  ├─ AssetCatalogService.cs
│     │  ├─ SerialDiscoveryService.cs
│     │  ├─ DeviceConnectionService.cs
│     │  ├─ DeviceProtocolService.cs
│     │  └─ DeploymentService.cs
│     │
│     ├─ Protocol/
│     │  ├─ AtlasLedCommand.cs
│     │  ├─ AtlasLedPacket.cs
│     │  ├─ PacketCodec.cs
│     │  └─ Crc32.cs
│     │
│     └─ wwwroot/
│        ├─ index.html
│        ├─ projects.html
│        ├─ editor.html
│        ├─ device.html
│        │
│        ├─ css/
│        │  ├─ tokens.css
│        │  ├─ app.css
│        │  ├─ home.css
│        │  ├─ projects.css
│        │  ├─ editor.css
│        │  └─ device.css
│        │
│        ├─ js/
│        │  ├─ app.js
│        │  ├─ core/
│        │  │  ├─ api-client.js
│        │  │  ├─ dom.js
│        │  │  ├─ errors.js
│        │  │  ├─ event-bus.js
│        │  │  └─ dialog.js
│        │  │
│        │  ├─ project/
│        │  │  ├─ project-model.js
│        │  │  ├─ project-store.js
│        │  │  └─ dirty-state.js
│        │  │
│        │  ├─ editor/
│        │  │  ├─ editor-app.js
│        │  │  ├─ editor-state.js
│        │  │  ├─ scene-renderer.js
│        │  │  ├─ framebuffer.js
│        │  │  ├─ pixel-mapper.js
│        │  │  ├─ canvas-viewport.js
│        │  │  ├─ tool-manager.js
│        │  │  ├─ history-manager.js
│        │  │  ├─ selection-manager.js
│        │  │  ├─ layer-controller.js
│        │  │  ├─ inspector-controller.js
│        │  │  ├─ scene-controller.js
│        │  │  ├─ timeline-controller.js
│        │  │  ├─ playback-clock.js
│        │  │  ├─ simulator-view.js
│        │  │  ├─ destination-controller.js
│        │  │  └─ deployment-controller.js
│        │  │
│        │  ├─ tools/
│        │  │  ├─ select-tool.js
│        │  │  ├─ pencil-tool.js
│        │  │  ├─ eraser-tool.js
│        │  │  ├─ line-tool.js
│        │  │  ├─ rectangle-tool.js
│        │  │  ├─ ellipse-tool.js
│        │  │  ├─ fill-tool.js
│        │  │  ├─ text-tool.js
│        │  │  ├─ icon-tool.js
│        │  │  ├─ emoji-tool.js
│        │  │  └─ image-tool.js
│        │  │
│        │  ├─ assets/
│        │  │  ├─ asset-picker.js
│        │  │  ├─ bitmap-font.js
│        │  │  ├─ text-rasterizer.js
│        │  │  ├─ svg-rasterizer.js
│        │  │  └─ image-rasterizer.js
│        │  │
│        │  └─ pages/
│        │     ├─ home-page.js
│        │     ├─ projects-page.js
│        │     └─ device-page.js
│        │
│        └─ assets/
│           ├─ icons/
│           ├─ emoji/
│           ├─ fonts/
│           ├─ catalogs/
│           └─ licenses/
│
├─ tests/
│  ├─ AtlasLetrero.Tests/
│  └─ AtlasLetrero.E2E/
│
├─ firmware/
│  └─ AtlasLED/
│
└─ tools/
   ├─ sync-assets.ps1
   ├─ verify-assets.ps1
   ├─ smoke.ps1
   └─ publish-win.ps1
```

No crear carpetas adicionales sin necesidad real.

---

# 4. LAS ÚNICAS 4 PANTALLAS

## 4.1 INICIO

Objetivo: entrar a trabajar en menos de 2 clics.

```text
┌──────────────────────────────────────────────────────────────┐
│ ATLAS LETRERO                              ○ Sin dispositivo │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                     ATLAS LETRERO                            │
│              Diseña tu letrero LED                          │
│                                                              │
│               [ + NUEVO PROYECTO ]                          │
│               [ ABRIR PROYECTO ]                            │
│                                                              │
│ Proyectos recientes                                          │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Taller                     32×16     Hoy       [Abrir]   │ │
│ │ Abierto 24H                64×16     Ayer      [Abrir]   │ │
│ └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

No meter configuración técnica aquí.

### Modal Nuevo Proyecto

TODOS los campos tienen label visible.

```text
┌──────────────────────────────────────────────┐
│ NUEVO PROYECTO                               │
├──────────────────────────────────────────────┤
│ Nombre             [Mi letrero___________]   │
│ Tecnología         [WS2812B / NeoPixel ▼]    │
│ Ancho              [32]                      │
│ Alto               [16]                      │
│ Cableado           [Serpentina ▼]            │
│ Origen             [Superior izquierda ▼]    │
│ Dirección          [Horizontal → ▼]          │
│ Orden de color     [GRB ▼]                   │
│ FPS                [12]                      │
│ Brillo máximo      [80 %]                    │
│                                              │
│               [Cancelar] [CREAR]             │
└──────────────────────────────────────────────┘
```

`CREAR`:
1. valida;
2. crea proyecto;
3. guarda;
4. navega directamente al Editor con ese proyecto.

---

## 4.2 PROYECTOS

```text
┌──────────────────────────────────────────────────────────────────┐
│ PROYECTOS                                     [Buscar_________]  │
├──────────────────────────────────────────────────────────────────┤
│ Taller         32×16     Hoy      [Abrir] [⋮]                   │
│ Abierto 24H    64×16     Ayer     [Abrir] [⋮]                   │
│                                                                  │
│ [+ Nuevo]                                                       │
└──────────────────────────────────────────────────────────────────┘
```

Menú `⋮`:
- Duplicar
- Renombrar
- Eliminar

Eliminar requiere confirmación.

---

## 4.3 EDITOR — PANTALLA PRINCIPAL

**El simulador debe estar visible permanentemente en esta misma pantalla.**
No texto sustituto. No otra página. No ocultarlo hasta que exista hardware.

Layout objetivo en escritorio 1920×1080:

```text
┌────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ATLAS LETRERO   [Guardar] [↶][↷]   Escena [Principal▼][+][Duplicar][✓Activa][Eliminar]        │
├─────────────┬────────────────────────────────────────────────┬───────────────────────────────────┤
│ HERRAMIENTAS│                                                │ SIMULADOR LOCAL                  │
│             │                                                │ ┌───────────────────────────────┐ │
│ Selección   │                                                │ │  PREVIEW LED SIEMPRE VISIBLE │ │
│ Lápiz       │                                                │ │                               │ │
│ Borrador    │                   CANVAS LED                   │ └───────────────────────────────┘ │
│ Línea       │                                                │ [◀][▶/⏸][■][▶] Loop[✓]          │
│ Rectángulo  │                                                │                                   │
│ Elipse      │                                                ├───────────────────────────────────┤
│ Relleno     │                                                │ INSPECTOR                         │
│ Texto       │                                                │ Objeto/Capa seleccionado          │
│ Icono       │                                                │ X Y W H / color / opacidad       │
│ Emoji       │                                                │ Efecto / velocidad               │
│ Imagen      │                                                │                                   │
│             │                                                ├───────────────────────────────────┤
│ COLOR       │                                                │ DESTINO                           │
│ [████]      │                                                │ [Simulador local ▼]               │
│ BRILLO      │                                                │ ESP32 ○ No conectado              │
│ [======--]  │                                                │ [Detectar] [Elegir]               │
│             │                                                │ [ ENVIAR ]                        │
│             │                                                │ Estado: Listo                     │
├─────────────┴────────────────────────────────────────────────┴───────────────────────────────────┤
│ CAPAS / OBJETOS                                                                                 │
│ 👁 🔓 Texto "ABIERTO"    👁 🔓 Icono PC    👁 🔒 Fondo    [+][Dup][↑][↓][Eliminar]             │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ TIMELINE / FRAMES                                                                                │
│ [|◀][◀][▶/⏸][■][▶][▶|]  Loop [✓]  FPS [12]  00:02.350 / 00:05.000                            │
│ Frame: 001      002      003      004      005      006                                          │
│        [■]──────[■]──────[■]──────[■]──────[■]──────[■]                                          │
│ [+ Frame] [Duplicar] [Eliminar]   Duración [100 ms]                                               │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Reglas de layout

- No horizontal scroll a 1920×1080.
- A 1366×768, permitir que Inspector/Destino se compacten, pero Simulator permanece visible.
- Canvas recibe el espacio central mayor.
- Timeline no debe ocupar media pantalla.
- Herramientas siempre a mano.
- No duplicar controles.
- No mostrar nombres técnicos en inglés.
- Si una función todavía no existe, botón `disabled` + tooltip; jamás aparentar funcionamiento.

---

## 4.4 CONFIGURACIÓN / DISPOSITIVO

```text
┌─────────────────────────────────────────────────────────────────┐
│ CONFIGURACIÓN / DISPOSITIVO                                    │
├─────────────────────────────────────────────────────────────────┤
│ MATRIZ                                                          │
│ Tecnología         [WS2812B / NeoPixel ▼]                       │
│ Ancho              [32]       Alto [16]                         │
│ Cableado           [Serpentina ▼]                               │
│ Origen             [Superior izquierda ▼]                       │
│ Dirección          [Horizontal → ▼]                             │
│ Orden de color     [GRB ▼]                                      │
│ Rotación           [0° ▼]                                       │
│ Flip X [ ]         Flip Y [ ]                                   │
│ FPS máximo         [12]                                         │
│ Brillo máximo      [80%]                                        │
│                                                                 │
│ DISPOSITIVO                                                     │
│ [Detectar USB/Serial]                                           │
│ Puerto             [COM5 ▼]                                     │
│ Estado             ○ No conectado                              │
│ Firmware           --                                           │
│ Protocolo          --                                           │
│ Resolución reportada --                                         │
│                                                                 │
│ [Conectar] [Desconectar] [PROBAR MATRIZ]                        │
└─────────────────────────────────────────────────────────────────┘
```

---

# 5. MODELO DE DOMINIO

## `ProjectDocument`

```text
Id
Name
FormatVersion
MatrixConfiguration
Scenes[]
ActiveSceneId
Palette[]
EmbeddedAssets[]
CreatedUtc
ModifiedUtc
```

## `MatrixConfiguration`

```text
Technology
Width
Height
WiringMode
Origin
PrimaryDirection
ColorOrder
Rotation
FlipX
FlipY
PreferredFps
BrightnessLimit
```

## `SceneDocument`

```text
Id
Name
Active
DurationMs
Layers[]
Frames[]
```

## `LayerDocument`

```text
Id
Name
Visible
Locked
Opacity
Order
Objects[]
```

## `ObjectDocument`

```text
Id
Type
Name
X
Y
Width
Height
Rotation
Opacity
Visible
Content
Style
Effect
```

Tipos:
`Drawing`, `Text`, `Icon`, `Emoji`, `Image`, `Group`.

---

# 6. FUENTE DE VERDAD DEL RENDER

No existirán “canvas lógico”, “simulador lógico” y “payload lógico” separados.

```text
OBJETOS + FRAME + TIEMPO
          ↓
    SceneRenderer
          ↓
     FrameBuffer
      ↙       ↘
CanvasView   SimulatorView
          ↓
      PixelMapper
          ↓
    Payload ESP32
```

`FrameBuffer` usa dimensiones lógicas de la matriz.

El zoom del Canvas jamás modifica dimensiones lógicas.

**Invariante de desarrollo:**

```text
hash(EditorFrameBuffer)
==
hash(SimulatorFrameBuffer)
==
hash(PayloadDeserializado)
```

para mismo proyecto/escena/frame/tiempo.

---

# 7. HERRAMIENTAS: AUTOMATIZADAS, NO CASTIGO PÍXEL POR PÍXEL

El lápiz existe únicamente para detalle fino.

## Selección
- click selecciona;
- drag crea selección;
- mover por drag;
- flechas mueven 1 píxel;
- Shift+flecha mueve 5;
- handles para escalar cuando aplique.

## Línea
PointerDown → PointerMove preview → PointerUp crea una línea.

## Rectángulo / Elipse
Arrastrar y soltar.

## Relleno
Flood fill limitado al framebuffer/capa.

## Texto
Click `Texto` abre modal, no cambia a un “modo misterioso”.

## Icono
Click `Icono` abre picker.

## Emoji
Click `Emoji` abre picker.

## Imagen
Click `Imagen` abre selector de archivo.

## Operaciones generales
- copiar;
- pegar;
- duplicar;
- eliminar;
- alinear izquierda/centro/derecha;
- centrar horizontal/vertical;
- ordenar arriba/abajo;
- bloquear;
- ocultar.

---

# 8. TEXTO LED DE VERDAD

No exigir dibujar letras.

Modal:

```text
Texto:       [SE ARREGLAN COMPUTADORAS____________]
Fuente LED:  [5x7 clásico ▼]
Escala:      [1x ▼]
Color:       [████]
Alineación:  [Izquierda ▼]
Espaciado:   [1 px]
Efecto:      [Marquee izquierda ▼]

PREVIEW:
┌─────────────────────────────────────────┐
│ SE ARREGLAN COMPUTADORAS →             │
└─────────────────────────────────────────┘

[Cancelar] [Insertar]
```

El objeto conserva el texto editable.

## Banco de fuentes

Necesidad real:
- bitmap 5×7;
- 5×8;
- 6×8;
- 8×8;
- alguna 8×12/8×13 legible.

**U8g2 puede servir como fuente de bancos bitmap, pero las fuentes de U8g2 tienen licencias distintas.**
No copiar “todo U8g2” ciegamente.

`tools/sync-assets.ps1` debe usar una whitelist con:
- nombre exacto;
- origen;
- licencia comprobada;
- archivo LICENSE/NOTICE.

Preferir familias X11/BDF u otras con términos redistribuibles claramente documentados.

Para UI general usar una única sans local (p.ej. Inter o Noto Sans) con licencia redistribuible.

No descargar fuentes góticas/decorativas que no aportan a LED.

---

# 9. ICONOS OPEN SOURCE

Banco local real.

Prioridad:

1. Bootstrap Icons — SVG, licencia MIT.
2. Tabler Icons — SVG, licencia MIT.

No necesitamos dos bancos gigantes visibles simultáneamente: el catálogo puede unificarlos.

Estructura:

```text
assets/icons/bootstrap/
assets/icons/tabler/
assets/catalogs/icons.json
assets/licenses/bootstrap-icons-LICENSE.txt
assets/licenses/tabler-icons-LICENSE.txt
```

`icons.json`:

```json
{
  "id": "computer",
  "displayName": "Computadora",
  "source": "bootstrap-icons",
  "path": "icons/bootstrap/pc-display.svg",
  "tags": ["pc", "computadora", "monitor", "reparación", "tecnología"]
}
```

Picker:
- búsqueda;
- categorías;
- favoritos;
- recientes;
- tamaño;
- color;
- preview normal;
- preview LED.

SVG se rasteriza localmente a un buffer lógico antes de insertar.

---

# 10. EMOJIS OPEN SOURCE

Usar UNO como banco principal para evitar duplicados visuales.

Recomendado:
- **Twemoji**: código MIT; gráficos CC-BY 4.0, requiere atribución.
Alternativa:
- **Noto Emoji**: fuentes OFL 1.1; la mayoría de recursos de imagen Apache 2.0.

Para simplificar producto:
- elegir Twemoji **o** Noto Emoji como principal;
- conservar licencias/atribución;
- no mezclar sin razón.

Nunca descargar emoji en runtime.

---

# 11. SCRIPT DE ASSETS OBLIGATORIO

`tools/sync-assets.ps1`

Debe:

1. descargar sólo repositorios/versiones permitidos;
2. guardar versión o commit usado;
3. verificar que exista LICENSE;
4. copiar únicamente assets necesarios;
5. generar catálogos JSON;
6. generar tags/sinónimos básicos en español;
7. dejar todo listo en `wwwroot/assets`;
8. no depender de Internet después.

`tools/verify-assets.ps1` falla si:
- falta catálogo;
- falta archivo referenciado;
- hay duplicados de ID;
- falta licencia;
- aparece URL `http://` o `https://` dentro de HTML/CSS/JS de runtime para assets.

---

# 12. IMÁGENES

Flujo:

```text
Abrir archivo
→ preview original
→ Contener / Cubrir / Recortar
→ brillo/contraste
→ reducir resolución
→ reducir colores
→ dithering opcional
→ preview LED
→ insertar como objeto
```

Formatos MVP:
PNG, JPG/JPEG, BMP.

No bloquear UI al procesar imágenes medianas.

---

# 13. CAPAS / OBJETOS

La barra `CAPAS / OBJETOS` es funcional, no texto.

Al seleccionar una capa:
- Inspector muestra propiedades de capa.

Al seleccionar objeto:
- Inspector muestra propiedades de objeto.

Botones:
- `+`
- Duplicar
- ↑
- ↓
- Eliminar

Visibilidad/bloqueo se aplican inmediatamente al Canvas y Simulator.

---

# 14. ESCENAS

En toolbar:

```text
Escena [Principal ▼] [+] [Duplicar] [✓ Activa] [Eliminar]
```

Nueva escena:
- solicita nombre;
- crea capa base + frame inicial.

Duplicar:
- copia profundamente contenido.

Eliminar:
- confirmación;
- impedir dejar proyecto sin ninguna escena.

---

# 15. TIMELINE Y PLAYBACK

Timeline mínimo útil.

Frames:
- crear;
- duplicar;
- eliminar;
- mover;
- seleccionar;
- cambiar duración.

Controles:
- inicio;
- anterior;
- Play/Pause;
- Stop;
- siguiente;
- final;
- Loop.

Semántica:
- Pause conserva tiempo.
- Stop detiene y regresa a 0.
- Playback usa timestamp real, no “contar renders”.

---

# 16. EFECTOS

MVP:
- Ninguno
- Parpadeo
- Marquee izquierda
- Marquee derecha
- Slide
- Fade
- Aparecer
- Desaparecer
- Pulso de brillo

Cada efecto es función pura de:
`objeto + tiempo + parámetros → estado renderizado`.

No guardar el resultado renderizado como fuente de verdad.

---

# 17. SIMULADOR: PERMANENTE Y REAL

**El panel debe existir desde que abre el Editor.**

No depende de ESP32.

Debe:
- dibujar cada LED como celda/punto;
- respetar resolución;
- actualizarse en cada modificación;
- reproducir animación;
- Play/Pause/Stop/Loop;
- permitir zoom del preview;
- mostrar `32×16`, FPS y tiempo actual.

Cuando el usuario pulsa `ENVIAR` con destino `Simulador local`:

1. compilar escena;
2. validar paquete;
3. entregarlo al transporte de simulador;
4. el simulador debe cargar el paquete recibido;
5. comparar checksum/hash;
6. sólo entonces mostrar `Correcto`.

Así `Enviar → Correcto` prueba realmente el pipeline, no un botón decorativo.

---

# 18. DESTINOS

Selector:

```text
Destino:
[ Simulador local ▼ ]
```

Estados:
- Simulador local: siempre disponible.
- ESP32 USB/Serial: sólo disponible tras handshake.
- ESP32 Wi-Fi: posterior a USB estable.

Nunca usar `navigator.onLine` para estado ESP32.

---

# 19. BACKEND: CONTRATOS API

## Health

`GET /api/health`

Respuesta:
```json
{"status":"ok"}
```

## Proyectos

```text
GET    /api/projects
POST   /api/projects
GET    /api/projects/{id}
PUT    /api/projects/{id}
DELETE /api/projects/{id}
POST   /api/projects/{id}/duplicate
```

## Assets

```text
GET /api/assets/icons?q=
GET /api/assets/emojis?q=
GET /api/assets/fonts
```

## Dispositivos

```text
GET  /api/devices/serial
POST /api/devices/connect
POST /api/devices/disconnect
GET  /api/devices/status
POST /api/devices/test-matrix
POST /api/devices/upload
```

Errores devuelven JSON consistente:

```json
{
  "code": "DEVICE_TIMEOUT",
  "message": "No se recibió respuesta del letrero.",
  "details": null
}
```

La UI muestra `message`, nunca stack trace.

---

# 20. PERSISTENCIA

## Rutas

Configuración/autosave/log:
`%LOCALAPPDATA%\AtlasLetrero\`

Proyectos por defecto:
`%USERPROFILE%\Documents\AtlasLetrero\Projects\`

## Formato `.atlasled`

Usar contenedor ZIP nativo (`System.IO.Compression`) para portabilidad:

```text
manifest.json
assets/
  ...
preview.png        # opcional
```

`manifest.json` contiene versión y documento.

Guardado atómico:
1. escribir archivo temporal;
2. validar que abre;
3. flush;
4. replace/rename;
5. conservar original si algo falla.

Autosave sólo cuando `dirty=true`.

Recovery compara timestamp original/autosave.

---

# 21. PIXEL MAPPER

`pixel-mapper.js` contiene implementación canónica de coordenadas lógicas.

Casos:
- row-major;
- serpentina;
- origen TL/TR/BL/BR;
- horizontal/vertical;
- rotación 0/90/180/270;
- flip X/Y.

Crear tabla de pruebas exacta.

Ejemplo serpentina 8×4:

```text
00 01 02 03 04 05 06 07
15 14 13 12 11 10 09 08
16 17 18 19 20 21 22 23
31 30 29 28 27 26 25 24
```

---

# 22. HARDWARE / PROTOCOLO

Discovery real:

```text
enumerar COM
→ abrir candidato con timeout
→ HELLO
→ validar MAGIC/versión
→ HELLO_ACK
→ GET_CAPABILITIES
→ mostrar dispositivo
```

No etiquetar un COM genérico como ESP32 antes.

Paquete:

```text
MAGIC
VERSION
COMMAND
SEQUENCE
PAYLOAD_LENGTH
PAYLOAD
CRC32
```

Comandos:

```text
HELLO
HELLO_ACK
GET_CAPABILITIES
CAPABILITIES
BEGIN_UPLOAD
FRAME_DATA
END_UPLOAD
VERIFY
ACTIVATE
PLAY
STOP
SET_BRIGHTNESS
PING
ACK
NACK
ERROR
```

Upload:
- timeout;
- cancelación;
- retry limitado;
- secuencia;
- CRC;
- no activar si verify falla.

---

# 23. FIRMWARE ESP32

`firmware/AtlasLED/`

MVP:
- driver de matriz;
- serial;
- parser de protocolo;
- handshake;
- capabilities;
- buffer temporal de upload;
- CRC;
- activación atómica;
- reproducción;
- brillo;
- status.

Si upload falla:
- descartar temporal;
- seguir reproduciendo último contenido válido.

No flasheo automático hasta que comunicación básica sea estable.

---

# 24. SERVICES C# — RESPONSABILIDAD EXACTA

## `AppPaths`
Resuelve y crea rutas conocidas. Ningún controller arma rutas a mano.

## `AtomicFileWriter`
Escritura segura temp/replace.

## `ProjectPackageService`
Lee/escribe `.atlasled`. Valida versión/formato.

## `ProjectIndexService`
Lista proyectos y recientes. No abre proyecto completo para cada fila si no hace falta.

## `AssetCatalogService`
Carga catálogos una vez, indexa y busca. No escanea miles de SVG por petición.

## `SerialDiscoveryService`
Enumera puertos y realiza sondeo HELLO con timeout/cancelación.

## `DeviceConnectionService`
Mantiene exactamente una conexión activa y estado real.

## `DeviceProtocolService`
Codifica/decodifica paquetes.

## `DeploymentService`
Coordina:
validate → compile → begin → frames → verify → activate.

---

# 25. JAVASCRIPT — RESPONSABILIDAD EXACTA

## `editor-state.js`
Único estado editable actual.

## `scene-renderer.js`
Convierte estado + tiempo a `FrameBuffer`.

## `framebuffer.js`
Buffer lógico RGB/RGBA.

## `canvas-viewport.js`
Sólo presentación/zoom/grid/pointer coordinate transform.

## `simulator-view.js`
Sólo muestra FrameBuffer o paquete recibido.

## `tool-manager.js`
Activa una herramienta a la vez.

## `history-manager.js`
Undo/Redo; un gesto de drag = una acción.

## `timeline-controller.js`
Frames/playhead.

## `playback-clock.js`
Tiempo y loop.

## `destination-controller.js`
Destino seleccionado + estado.

## `deployment-controller.js`
Orquesta UI de envío y progreso.

**Nunca interpolar una Promise al DOM.**
Toda función async debe resolverse antes del render.

---

# 26. PREVENCIÓN EXPLÍCITA DE `[object Promise]`

Crear helper:

```js
export async function renderAsync(target, producer) {
    const value = await producer();
    target.replaceChildren(value);
}
```

o equivalente seguro.

Prueba E2E obligatoria:

- navegar a 4 pantallas;
- `body.innerText` NO contiene:
  - `[object Promise]`
  - `[object Object]`
  - `undefined`
  - `null` como placeholder visible.

Capturar consola:
- ningún `console.error`;
- ningún `pageerror`.

Capturar red:
- ningún 404/500 inesperado.

---

# 27. PRUEBAS AUTOMATIZADAS QUE SÍ VALEN

## Unitarias

- PixelMapper.
- CRC32/PacketCodec.
- ProjectPackageService.
- AtomicFileWriter.
- Drawing algorithms.
- Text bitmap raster.
- Timeline timing.
- Effects deterministic.

## Integración

- Save/Open roundtrip.
- Autosave/Recovery.
- Asset catalog.
- Simulator transport.
- Device protocol con fake transport.
- Upload failure/retry.

## E2E CON PLAYWRIGHT .NET

**Dev/test only.**

Escenarios:

### E2E-01 Inicio
- carga;
- no errores JS;
- abre Nuevo Proyecto;
- labels completos.

### E2E-02 Crear
- crea 32×16;
- navega a Editor;
- Canvas visible;
- Simulator visible.

### E2E-03 Dibujo
- seleccionar Línea;
- arrastrar;
- framebuffer cambia;
- Canvas cambia;
- Simulator cambia.

### E2E-04 Texto
- abrir Texto;
- escribir `HOLA Ñ`;
- insertar;
- objeto aparece;
- Simulator refleja.

### E2E-05 Capas
- nueva capa;
- duplicar;
- ocultar;
- Simulator cambia.

### E2E-06 Timeline
- duplicar frame;
- editar segundo;
- Play;
- preview cambia;
- Stop vuelve a inicio.

### E2E-07 Guardado
- guardar;
- recargar;
- proyecto visualmente igual.

### E2E-08 Enviar simulador
- seleccionar Simulador local;
- Enviar;
- paquete recibido;
- checksum válido;
- estado `Correcto`.

### E2E-09 Configuración
- todos los campos visibles;
- Detectar serial no inventa dispositivo.

### E2E-10 Sanidad
- no `[object Promise]`;
- no console errors;
- no 404/500.

---

# 28. SCRIPT `tools/smoke.ps1`

Debe:

1. `dotnet restore`
2. `dotnet build -c Release`
3. iniciar app en puerto conocido/dinámico;
4. esperar `/api/health`;
5. ejecutar E2E smoke;
6. detener proceso;
7. devolver exit code distinto de 0 ante fallo.

El agente debe usar este script antes de declarar “terminado”.

---

# 29. ORDEN DE IMPLEMENTACIÓN — 6 BLOQUES, NO 50 MINI-FASES

La especificación se ejecuta en **bloques verticales** para evitar scaffolds inútiles.

## BLOQUE A — ESQUELETO + NAVEGACIÓN FUNCIONAL
Entregable:
- solución .NET;
- 4 HTML;
- navegación;
- estilos;
- API health;
- modal Nuevo Proyecto completo;
- ninguna Promise visible;
- Playwright smoke.

No añadir botones fingidos. Lo futuro queda disabled.

## BLOQUE B — EDITOR FUNCIONAL COMPLETO BÁSICO
Entregable:
- proyecto real;
- Canvas;
- Simulator visible;
- herramientas dibujo;
- color/brillo;
- Undo/Redo;
- capas;
- inspector;
- escenas básicas.

Prueba real:
`crear → línea → simulator cambia`.

## BLOQUE C — CONTENIDO AUTOMÁTICO
Entregable:
- sync-assets;
- fuentes bitmap verificadas;
- iconos;
- emoji;
- texto;
- imagen.

Prueba real:
`texto + icono + emoji + PNG`, sin pintar manualmente píxeles.

## BLOQUE D — TIMELINE / EFECTOS / PERSISTENCIA
Entregable:
- frames;
- playback;
- efectos;
- save/open;
- autosave/recovery.

Prueba real:
animación reproduce, guardar/cerrar/abrir conserva resultado.

## BLOQUE E — DESTINO / SIMULADOR COMO TRANSPORTE
Entregable:
- pipeline de compilación;
- `Enviar` al simulador;
- verify/hash;
- progreso y resultado verídico.

Prueba real:
estado `Correcto` sólo tras recepción y verificación.

## BLOQUE F — ESP32
Entregable:
- configuración completa;
- serial discovery;
- handshake;
- protocolo;
- firmware MVP;
- envío real si hardware presente;
- Wi-Fi sólo después.

Si no hay hardware:
- probar protocolo con fake transport;
- reportar claramente “HIL no ejecutado”.

**No detenerse a pedir autorización entre bloques.**

---

# 30. GATE DE CALIDAD DESPUÉS DE CADA BLOQUE

Antes de continuar, el propio agente debe responder internamente:

```text
¿Puedo ejecutar el flujo desde UI?
¿El resultado es visible?
¿El simulador está visible?
¿Hay algún botón que no haga lo que promete?
¿Hay console errors?
¿Hay HTTP 4xx/5xx?
¿Hay [object Promise]/undefined/null?
¿El estado mostrado es verdadero?
¿Persistí lo que el usuario espera?
¿La pantalla coincide con el layout?
```

Si una respuesta falla:
**corregir antes de avanzar.**

No imprimir una novela; actualizar `BUILD_PROGRESS.md`:

```text
A | PASS | nav + nuevo proyecto + smoke
B | PASS | draw line -> simulator updated
...
```

---

# 31. CRITERIOS VISUALES DE ACEPTACIÓN

Editor NO puede verse como:
- formulario administrativo;
- página vacía con botones;
- canvas gigante sin controles;
- inspector vacío;
- simulator textual.

Debe verse como una herramienta gráfica compacta.

A 1920×1080:
- Canvas + Simulator + Inspector visibles simultáneamente.
- Timeline visible sin scroll vertical excesivo.
- No scroll horizontal.

Todos los controles visibles para usuario están en español.

---

# 32. ESTADOS REALES

## Device
```text
○ Sin dispositivo
◌ Detectando…
● ESP32 conectado
⚠ Error
```

## Deployment
```text
Listo
Validando
Preparando
Enviando 42%
Verificando
Activando
Correcto
Error
Cancelado
```

No reutilizar `Correcto` de una ejecución anterior.

Cada envío crea un nuevo estado.

---

# 33. ERRORES Y RECUPERACIÓN

UI:

```text
No se pudo guardar el proyecto.
El archivo original no fue modificado.

[Reintentar] [Ver detalles]
```

No stacktrace directo.

Casos a probar:
- archivo bloqueado;
- ruta inválida;
- proyecto corrupto;
- asset faltante;
- COM ocupado;
- desconexión;
- timeout;
- NACK;
- CRC incorrecto.

---

# 34. LICENCIAS / ATRIBUCIÓN

Crear pantalla/modal dentro de `Ayuda → Recursos y licencias`.

Registrar exactamente lo descargado.

Notas:
- Bootstrap Icons: MIT.
- Tabler Icons: MIT.
- Twemoji: código MIT; gráficos CC-BY 4.0, incluir atribución.
- Noto Emoji: revisar el tipo de recurso usado; fonts y recursos de imagen no comparten necesariamente la misma licencia.
- U8g2: el código de la librería tiene licencia BSD-2-Clause, pero **las fuentes individuales pueden tener licencias distintas**. No asumir licencia uniforme.

Si la licencia de una fuente concreta no está clara:
**no incluir esa fuente.**

---

# 35. PUBLICACIÓN WINDOWS

`tools/publish-win.ps1`

Publicar self-contained o framework-dependent según tamaño/objetivo, pero usuario no necesita Visual Studio.

Launcher:
- abre servidor loopback sólo en `127.0.0.1`;
- abre navegador;
- evita múltiples instancias si es posible;
- cierre limpio.

No exponer server a LAN por defecto.

---

# 36. PRUEBA REINA — NO NEGOCIABLE

Desde una instalación limpia:

1. Abrir Atlas Letrero.
2. Nuevo proyecto.
3. Nombre `Taller`.
4. 32×16, serpentina, GRB, 12 FPS.
5. Crear.
6. Editor abre.
7. Simulator está visible sin tocar nada.
8. Dibujar una línea.
9. Simulator cambia.
10. Insertar texto `SE ARREGLAN COMPUTADORAS`.
11. Elegir fuente bitmap LED.
12. Insertar icono de computadora del banco local.
13. Insertar emoji del banco local.
14. Importar PNG.
15. Crear segunda capa.
16. Ocultar/mostrar capa y ver respuesta.
17. Duplicar frame.
18. Cambiar frame 2.
19. Play: preview anima.
20. Pause: conserva tiempo.
21. Stop: vuelve a inicio.
22. Aplicar marquee.
23. Guardar.
24. Recargar.
25. Todo permanece.
26. Seleccionar `Simulador local`.
27. `ENVIAR`.
28. Pipeline compila, transporta, verifica.
29. Estado `Correcto`.
30. Abrir Configuración.
31. Todos los campos existen.
32. Detectar USB/Serial no inventa ESP32.
33. Volver al Editor sin perder proyecto.

Sólo después se considera MVP funcional.

---

# 37. INSTRUCCIÓN FINAL PARA EL AGENTE

```text
Lee este documento completo UNA VEZ.

Implementa Atlas Letrero desde cero siguiendo los 6 BLOQUES en orden.
NO te detengas a pedir permiso entre bloques.
NO conviertas el documento en una lista de placeholders.
Cada bloque debe cerrar un flujo de usuario real.

Reglas críticas:
- 4 pantallas máximo.
- Editor es el centro.
- Simulador SIEMPRE visible en Editor.
- No Razor/Forms/Vue/Angular/React.
- No CDN.
- Assets open source descargados y locales.
- No hardcodear demos para aparentar avance.
- No botón sin comportamiento.
- No estado falso.
- No [object Promise].
- No declarar éxito sólo por build/tests.
- Ejecutar Playwright/smoke y verificar navegador.
- No mutation testing.
- No commit/push salvo orden explícita.

Si un test de UI falla, corrige el producto, no el test para que pase.

Al terminar:
1. ejecutar tools/smoke.ps1;
2. ejecutar build Release;
3. ejecutar pruebas E2E;
4. ejecutar la PRUEBA REINA;
5. reportar sólo hechos verificados;
6. listar limitaciones reales, especialmente si no hubo ESP32 físico.
```

---

# 38. FUENTES DE CRITERIO UTILIZADAS PARA ESTA ESPECIFICACIÓN

Se incorporaron principios de:
- NASA Human Factors / Crew Interfaces: agrupación lógica de controles, relación control-display, información crítica visible simultáneamente y flujo de pantalla alineado a la tarea.
- NASA Display Standard: evitar scroll horizontal, consistencia entre pantallas, información directamente utilizable y salvaguardas para acciones irreversibles.
- U8g2: disponibilidad de familias bitmap 5×7/5×8/8×8 y advertencia de que las licencias varían por fuente.
- Bootstrap Icons y Tabler Icons: bancos SVG open source con licencia MIT.
- Twemoji y Noto Emoji: bancos open source con requisitos de licencia/atribución distintos según recurso.

**El objetivo de estas referencias no es hacer Atlas Letrero “como la NASA”; es evitar una UI desordenada, estados ambiguos y controles separados de su resultado.**

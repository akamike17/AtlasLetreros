# AtlasLetrero --- Especificación Maestra para Codex

## 0. DIRECTIVA DE EJECUCIÓN Y AHORRO EXTREMO DE TOKENS

Construye **AtlasLetrero** como producto comercial real, modular,
mantenible y comprobable. Este documento es el contrato de construcción.

**AHORRA LA MAYOR CANTIDAD DE TOKENS POSIBLE.** - Lee este documento
completo una vez y crea internamente el plan de trabajo. - No repitas
requisitos ni vuelvas a explicar lo que ya está escrito. - No narres
cada archivo, comando o cambio. - No produzcas reportes largos durante
la ejecución. - Responde con estados mínimos: bloque realizado, pruebas
ejecutadas, fallos reales y siguiente bloque. - No preguntes decisiones
ya resueltas aquí. - Inspecciona antes de crear para no duplicar
código. - Reutiliza abstracciones y componentes. - No generes archivos,
capas, DTOs, interfaces ni servicios ornamentales. - No reescribas
código correcto sin necesidad. - No gastes tokens celebrando builds
exitosos. - Ante un error, lee la salida real, identifica causa, corrige
y vuelve a probar. - Si una corrección segura es evidente, ejecútala; no
te detengas sólo en el diagnóstico. - No agregues paquetes ni cambies
arquitectura sólo para forzar un build. - Revierte cambios propios si la
evidencia demuestra que fueron incorrectos. - No declares éxito hasta
validar los flujos funcionales. - Trabaja hasta completar el alcance V1
o encontrar un bloqueo externo real imposible de resolver desde el
repositorio.

Secuencia obligatoria:

`READ → EDIT/WRITE → READ/VERIFY → BUILD/TEST → CORRECT → RETEST`

En PowerShell usar `;` cuando corresponda en lugar de depender de `&&`.

No mencionar IA, agentes ni herramientas generativas en producto, UI,
repositorio, documentación pública, commits ni artefactos finales.

------------------------------------------------------------------------

# 1. VISIÓN DEL PRODUCTO

AtlasLetrero es una plataforma para diseñar, configurar, administrar y
reproducir **letreros LED comerciales económicos**.

Objetivo inicial: pequeños negocios que necesitan anuncios dinámicos
como:

`SE ARREGLAN COMPUTADORAS`

`TACOS 2X1`

`HAMBURGUESAS EL MIKE`

El valor comercial no es solamente ensamblar LEDs: es combinar hardware
económico con firmware, editor, configuración sencilla, funcionamiento
autónomo y compatibilidad con distintos paneles/controladores.

## 1.1 BASIC

-   Letrero configurado por nosotros.
-   Escena almacenada localmente.
-   Arranca y reproduce automáticamente.
-   No necesita PC después de configurarlo.
-   No necesita Internet.
-   Puede trabajar sin router.
-   Cambio posterior de anuncio puede venderse como servicio.
-   Firmware genérico: **no reflashear firmware por cada anuncio**.

## 1.2 SMART

-   Mismo núcleo de hardware cuando sea posible.
-   Wi-Fi.
-   Editor.
-   Cambio de texto, colores, imágenes y animaciones.
-   Playlists.
-   Horarios.
-   Administración de varios letreros.
-   BASIC debe poder convertirse en SMART sin cambiar hardware cuando
    las capacidades físicas lo permitan.

------------------------------------------------------------------------

# 2. PRINCIPIO CENTRAL DE ARQUITECTURA

Nunca acoplar el producto a un microcontrolador, panel o librería
concreta.

``` text
Scene Engine
    ↓
Logical FrameBuffer
    ↓
Matrix Mapper
    ↓
Output Driver API
    ↓
Generic / Known Profile / Specialized / Custom
    ↓
Controller
    ↓
Physical Display
```

C#/.NET crea y administra contenido.

El firmware ejecuta contenido y controla hardware.

El panel únicamente representa la salida.

C# **nunca** debe conocer constantes eléctricas/librerías como
`NEO_GRB`, RMT, HUB75 DMA, GPIO específicos, etc.

------------------------------------------------------------------------

# 3. SOLUCIÓN / CAPAS

Crear una solución organizada aproximadamente como:

``` text
src/
  AtlasLetrero.Domain
  AtlasLetrero.Application
  AtlasLetrero.Infrastructure
  AtlasLetrero.Protocol
  AtlasLetrero.Presentation

firmware/
  AtlasLetrero.Native

simulator/
  AtlasLetrero.Simulator

tests/
  AtlasLetrero.Domain.Tests
  AtlasLetrero.Application.Tests
  AtlasLetrero.Infrastructure.Tests
  AtlasLetrero.Protocol.Tests
  AtlasLetrero.Simulator.Tests
  AtlasLetrero.IntegrationTests
```

Ajustar nombres sólo cuando exista una razón técnica clara.

## Domain

Modelo puro, invariantes, escenas, geometría, matrices, capacidades.

## Application

Casos de uso, contratos, validación y orquestación.

## Infrastructure

Persistencia, networking, discovery, adaptadores/controladores externos.

## Protocol

Contrato versionado PC/controller.

## Presentation

Editor y administración.

## Firmware

Runtime nativo del controlador.

## Simulator

Display virtual que reutiliza contratos reales.

## Tests

Pruebas funcionales por nivel.

Dependencias deben apuntar hacia el dominio, nunca al revés.

------------------------------------------------------------------------

# 4. MODELO DE DOMINIO

Crear sólo las piezas que tengan comportamiento/responsabilidad real.

Conceptos mínimos:

``` text
Business
Device
DeviceIdentity
DeviceManifest
ControllerProfile
ControllerCapabilities
DisplayProfile
MatrixTile
MatrixTopology
PowerProfile
NetworkConfiguration
Scene
Layer
SceneElement
TextElement
ShapeElement
ImageElement
Animation
Transition
Playlist
PlaylistItem
Schedule
FontProfile
FirmwareVersion
CompatibilityCertification
```

Relaciones:

``` text
Business
 └─ Devices
     ├─ ControllerProfile
     ├─ DisplayProfile
     │   └─ MatrixTiles
     ├─ PowerProfile
     └─ Scenes / Playlists / Schedules

Scene
 └─ Layers
     └─ Elements
         └─ Animations
```

No crear entidades vacías para aparentar arquitectura.

------------------------------------------------------------------------

# 5. CANVAS / EDITOR

Canvas lógico independiente del hardware.

Funciones V1:

``` text
New/Open/Save design
Pencil
Eraser
Line
Rectangle
Circle
Fill
Text
Image
Selection
Move
Undo
Redo
Zoom
Grid
Color
Brightness
Animation controls
Duration
Speed
Preview
Device target
Send/Play/Stop
```

Mostrar claramente píxeles encendidos/apagados.

El mismo diseño debe poder mapearse a hardware compatible diferente sin
rehacer el contenido.

------------------------------------------------------------------------

# 6. TEXTO Y LEGIBILIDAD

Incluir fuentes LED optimizadas:

``` text
3x5
5x7
8x8
larger low-resolution fonts
```

Prioridad: **legibilidad**.

Implementar suavizado únicamente cuando la resolución y tecnología lo
permitan.

No usar sombras falsas para fingir antialiasing.

Soportar alineación, spacing, scrolling y clipping correctamente.

------------------------------------------------------------------------

# 7. ANIMACIONES

Motor reutilizable con:

``` text
Blink
Fade
Scroll
Slide
Zoom
Pulse
Wipe
Marquee
FrameAnimation
Transitions
Repeat
Duration
Speed
Easing cuando aporte valor
```

Debe funcionar tanto en preview como en runtime real.

------------------------------------------------------------------------

# 8. MODOS DE CONTENIDO

## Realtime

C# renderiza frames y transmite. Requiere host activo.

## Autonomous Scene

C# compila/serializa escena. Controller almacena y reproduce
independientemente.

## Pre-rendered

C# genera frames optimizados/comprimidos y controller los reproduce.

Preferir modo autónomo para BASIC.

------------------------------------------------------------------------

# 9. FRAMEBUFFER

Modelo lógico de píxeles.

Soportar como mínimo:

``` text
RGB
RGBW cuando hardware lo permita
brightness
width
height
pixel addressing X/Y
```

Toda herramienta de dibujo/animación produce FrameBuffer lógico.

------------------------------------------------------------------------

# 10. MATRIX MAPPER

Debe resolver:

``` text
width
height
origin:
  TopLeft
  TopRight
  BottomLeft
  BottomRight

traversal:
  row
  column

layout:
  progressive
  serpentine

rotation:
  0
  90
  180
  270

mirror:
  horizontal
  vertical
  both

color order:
  RGB
  GRB
  BRG
  etc.

RGBW

tilesX
tilesY
tile order
tile rotation
panel chaining
```

Múltiples matrices físicas deben poder formar un solo canvas lógico.

Ejemplos:

``` text
16x16

16x16 + 16x16
= 32x16

4 x 16x16
= 32x32
```

------------------------------------------------------------------------

# 11. DRIVER API

Contrato equivalente a:

``` csharp
public interface IDisplayDriver
{
    string Id { get; }
    DriverCapabilities GetCapabilities();
    Task InitializeAsync(DeviceConfiguration config);
    Task SetBrightnessAsync(byte brightness);
    Task RenderAsync(FrameBuffer frame);
    Task ClearAsync();
    Task TestAsync();
}
```

No copiar necesariamente este código si el diseño final requiere tipos
mejores; conservar responsabilidad.

Driver Registry inicial:

``` text
generic.addressable
generic.clocked
hub75
max7219
custom
```

------------------------------------------------------------------------

# 12. GENERIC / KNOWN / CUSTOM

## Generic Addressable

Paneles/tiras one-wire configurables.

Parámetros: - GPIO - timing/frequency - pixel count - color order -
channel count - reset timing - brightness/current limits.

## Generic Clocked

DATA+CLOCK.

Familias objetivo: - APA102 - SK9822 - WS2801 - compatibles.

## HUB75

Driver especializado.

## Known Profile

Un modelo conocido debe ser principalmente un preset del driver genérico
cuando sea posible, evitando duplicar implementación.

Ejemplo conceptual:

`WS2812B = generic.addressable + 800kHz + GRB + profile`

## Custom

Extensión para hardware/vendor no cubierto.

------------------------------------------------------------------------

# 13. COMPATIBILIDAD INICIAL

Prioridad V1:

``` text
WS281x
SK6812
APA102
HUB75
```

Preparar arquitectura para:

``` text
MAX7219/MAX7221
HT16K33
otros addressable/clocked
vendor-specific controllers
```

No declarar "certificado" sólo porque una librería upstream lo soporte.

------------------------------------------------------------------------

# 14. BACKENDS / SDKs DE REFERENCIA

No atar AtlasLetrero a una sola librería.

Evaluar/utilizar según necesidad:

``` text
ESP-IDF led_strip / RMT / SPI
FastLED
NeoPixelBus
ESP32-HUB75-MatrixPanel-DMA
SmartMatrix
Adafruit NeoPixel / NeoMatrix / GFX
MD_MAX72XX
Adafruit HT16K33
WLED como referencia de arquitectura/protocolos
```

Preferencia: - ESP-IDF/RMT como base genérica donde corresponda. -
FastLED/NeoPixelBus para ampliar chipsets cuando aporte valor. - HUB75
DMA para HUB75.

------------------------------------------------------------------------

# 15. CONTROLLER RUNTIME

No llamar conceptualmente a esta capa "ESP32 layer".

Categorías:

## Atlas Native

Firmware Atlas.

Objetivos iniciales:

``` text
ESP32
ESP32-S3
```

ESP32-S3 con PSRAM es controlador principal de desarrollo/cargas
mayores.

Arquitectura preparada para:

``` text
RP2040/Pico W
RP2350/Pico 2 W
STM32
nRF52
Raspberry Pi/Zero
```

No implementar todos en V1.

## Atlas Compatible

Adaptadores de red/protocolo:

``` text
DDP
Art-Net
sACN/E1.31
WLED cuando sea viable
```

DDP tiene prioridad inicial para realtime compatible.

## Atlas Custom

Adaptador específico.

------------------------------------------------------------------------

# 16. HARDWARE CAPABILITY RESOLVER

Implementar servicio que determine si un controlador puede ejecutar un
proyecto.

Considerar:

``` text
resolution
pixel count
color model
required FPS
memory
PSRAM
scene storage
driver family
number of outputs
network capability
animation complexity
```

Objetivo comercial: seleccionar **el controlador más barato que cumpla
el trabajo**.

Ejemplo:

``` text
16x16 WS2812
animaciones normales
→ ESP económico suficiente
```

Mientras:

``` text
múltiples HUB75
frames pesados
gran almacenamiento
→ ESP32-S3 + PSRAM
```

No reducir artificialmente funcionalidades del controlador barato. Las
diferencias deben provenir de límites físicos reales.

------------------------------------------------------------------------

# 17. ESTRATEGIA DE HARDWARE COMERCIAL

Laboratorio: - ESP32-S3 Pro/PSRAM. - Mantener varias placas de respaldo.

Producción: - usar ESP económico cuando satisfaga capacidades; - usar S3
cuando el proyecto lo requiera; - si panel/controlador incluido ya es
compatible, reutilizarlo y no agregar ESP innecesario.

Optimizar **costo total funcional**, no precio aislado del panel.

------------------------------------------------------------------------

# 18. FIRMWARE ATLAS NATIVE

Módulos V1:

``` text
Boot
Device Identity
Configuration
Wi-Fi Provisioning
Network
HTTP API
WebSocket/realtime transport
Protocol Version
Capabilities
Scene Storage
Scene Player
Playlist Player
Scheduler
Frame Receiver
Matrix Mapper
Driver Registry
Power Manager
Diagnostics
Logging
OTA
Recovery
```

Firmware y contenido son independientes.

Nunca reflashear firmware sólo para cambiar "TACOS 2X1" por otro
anuncio.

------------------------------------------------------------------------

# 19. API LOCAL

Contrato versionado.

Endpoints mínimos conceptuales:

``` text
GET  /api/status
GET  /api/capabilities
GET  /api/config

POST /api/config
POST /api/frame
POST /api/scene
POST /api/play
POST /api/stop
POST /api/brightness
```

Añadir endpoints necesarios para scenes/playlists/storage/diagnostics
sin crear API inflada.

Ejemplo capabilities:

``` json
{
  "protocolVersion": 1,
  "device": "AtlasSign32",
  "display": {
    "width": 32,
    "height": 16,
    "colorModel": "RGB",
    "brightness": true
  },
  "features": [
    "frames",
    "scenes",
    "text",
    "playlist",
    "storage"
  ]
}
```

JSON aceptable para desarrollo/configuración.

Preparar formato binario compacto para frames/escenas cuando sea
necesario.

------------------------------------------------------------------------

# 20. PROVISIONING

Dispositivo nuevo:

``` text
Boot
 ↓
No Wi-Fi config
 ↓
AP: AtlasLED-XXXX
 ↓
Portal local
 ↓
Usuario selecciona Wi-Fi
 ↓
Guarda credenciales
 ↓
Reboot
 ↓
LAN
```

Requisitos: - Internet no obligatorio. - funcionamiento local-first; -
modo AP directo sin router; - discovery LAN; - mDNS cuando sea viable; -
nombre tipo `atlasled-A82F.local`.

Una escena almacenada debe reproducirse aunque: - PC esté apagada; -
router desaparezca; - Internet no exista.

Cloud queda fuera de V1.

------------------------------------------------------------------------

# 21. DETECCIÓN / CONFIGURACIÓN DEL PANEL

**No inventar autodetección.**

Paneles pasivos como WS2812B normalmente no informan modelo, dimensiones
u orientación.

Implementar:

## Atlas Preconfigured

Manifest guardado en controller.

## Known Profile

Usuario selecciona panel conocido.

## Generic Setup

Configura:

``` text
family
pixel count
width
height
GPIO
color order
layout
origin
rotation
```

## Guided Wizard

Mostrar patrones: - first pixel - corners - row/column - color -
traversal.

Preguntar qué observa el usuario e inferir configuración.

## Custom Advanced

Exponer sólo cuando aplique:

``` text
timing
frequency
channel order
bit depth
clock
latch
reset
```

Auto-discovery real sólo cuando el hardware/protocolo lo permita.

------------------------------------------------------------------------

# 22. SIMULADOR PROPIO

No depender exclusivamente de Wokwi.

Crear simulador que use exactamente:

``` text
Scene Engine
FrameBuffer
Matrix Mapper
Protocol models
```

No crear una animación visual falsa separada del motor real.

Debe mostrar: - pixels on/off - grid - canvas - tiles - orientation -
mapping - animations - brightness approximation - received frames.

------------------------------------------------------------------------

# 23. PREVIEW FÍSICO

Preparar perfiles:

``` text
RAW LED
PIXEL GRID
DIFFUSER
BLACK/SMOKED DIFFUSER
```

DisplayPhysicalProfile puede contener:

``` text
LED pitch
apparent LED size
diffuser distance
pixel aperture
spread
brightness limit
viewing profile
```

No convertir esta simulación óptica en bloqueo de V1.

------------------------------------------------------------------------

# 24. POWER MANAGER

Perfil:

``` text
supply voltage
available current
controller reserve
pixel count
display family
maximum brightness
```

Estimar consumo del frame y limitar brillo/corriente.

Referencia para WS2812B: - máximo teórico aproximado tradicional: \~60
mA/pixel en blanco completo; - 256 LEDs ≈ 15.36 A teóricos; - fuente
5V/10A puede utilizarse con límite de potencia/brillo apropiado.

No asumir que software reemplaza fusible, cable correcto o protecciones.

------------------------------------------------------------------------

# 25. FUENTES Y BUCK

Configuración comercial inicial preferida cuando corresponda:

``` text
5V / 10A / 50W
```

Fuentes de menor corriente pueden utilizarse para pruebas o con límites
adecuados.

Alternativa:

``` text
12V PSU
 ↓
BUCK FÍSICO 12V→5V
 ↓
5V BUS
 ├─ Controller
 └─ LED panel
```

El buck es hardware físico, no función de software.

Nunca:

``` text
12V → panel WS2812B 5V
12V → entrada 5V ESP32
```

Para salida 5V/10A, seleccionar buck realmente capaz de aproximadamente
50W continuos con margen y eficiencia adecuada.

Presupuesto preliminar para buck 10A: aproximadamente MXN \$200--250;
validar precio/capacidad al comprar.

Si ya existe una fuente 12V adecuada reutilizable, buck puede mejorar
costo.

Si todo se compra nuevo, comparar contra fuente 5V directa.

------------------------------------------------------------------------

# 26. ALIMENTACIÓN FÍSICA

Panel grande NO debe alimentarse a través del pin 5V/USB del ESP32.

Esquema:

``` text
PSU +5V ───── Panel +5V
       └───── Controller input apropiado

PSU GND ───── Panel GND
       └───── Controller GND

Controller GPIO ─── Panel DIN
```

GND común obligatorio cuando corresponda.

Para múltiples paneles: - DOUT → DIN para datos cuando la topología lo
requiera; - distribuir/inyectar potencia correctamente; - no hacer pasar
toda la corriente ciegamente por conectores pequeños.

------------------------------------------------------------------------

# 27. HARNESS / CONECTORES

Producto debe ser reparable.

Preferir: - terminales de tornillo; - JST/prefabricados; - conectores
desmontables; - strain relief; - heat shrink; - calibre correcto; -
fusible/protección; - harness estandarizado.

Evitar soldadura innecesaria en conexiones de servicio.

El cliente debe ver idealmente un único conector/cable de alimentación y
no un prototipo Arduino expuesto.

------------------------------------------------------------------------

# 28. CARCASA

Primer prototipo económico: - madera/MDF; - frente removible; -
electrónica protegida; - acceso de servicio; - ventilación cuando
corresponda.

Frentes intercambiables:

``` text
clear acrylic/mica
smoked/black translucent
opal/frosted
pixel grid + diffuser
```

No fijar dimensiones hasta medir panel físico real.

Un panel "16x16" no garantiza dimensiones mecánicas universales.

Diseño modular:

``` text
1 x 16x16
2 x 16x16 → 32x16
4 x 16x16 → 32x32
```

Separar visualmente/ físicamente: - display chamber; -
controller/cabling; - power access.

Preferir adaptador AC externo para que dentro de carcasa artesanal entre
bajo voltaje.

------------------------------------------------------------------------

# 29. COSTO / BOM

Mantener BOM por producto con:

``` text
component
supplier
unit price
volume price
rated capacity
measured/tested capacity
quantity
waste
final unit cost
```

Presupuesto inicial de referencia del primer prototipo: alrededor de
**MXN \$1,500**, sujeto al panel y componentes finalmente elegidos.

Carcasa artesanal preliminar: \~MXN \$300.

PC/herramientas de desarrollo son reutilizables y no se cargan como
hardware por unidad.

ESP32 sólo es reutilizable si el panel/controlador final puede funcionar
autónomamente sin él. En panel pasivo WS2812, el controller permanece en
el producto.

Objetivo posterior: reducir BOM mediante: - compras por volumen; -
controlador mínimo suficiente; - panel con controlador compatible cuando
convenga; - harness estandarizado; - carcasa repetible; - reutilización
segura de fuentes existentes.

------------------------------------------------------------------------

# 30. MODELO DE NEGOCIO SOPORTADO

Producto debe permitir:

``` text
BASIC configured sign
SMART editable sign
BASIC → SMART upgrade
Reconfiguration service
Custom animations
Controller + software
Software for compatible customer hardware
Multiple signs per customer
Repeat-customer pricing
```

No codificar precios fijos en dominio.

------------------------------------------------------------------------

# 31. SEGURIDAD

Implementar proporcionalmente al producto:

``` text
unique device identity
local authentication/pairing
protected Wi-Fi credentials
payload limits
input validation
protocol version validation
firmware integrity
safe OTA
rollback/recovery
configuration backup/recovery
```

Nunca aceptar frames/scenes ilimitados sin validar tamaño.

------------------------------------------------------------------------

# 32. PERSISTENCIA

Controller debe conservar como mínimo:

``` text
device identity
network config
display config
power config
active scene
stored scenes
playlist
schedule
brightness
firmware/config versions
```

Escrituras deben tolerar reinicios y evitar corrupción razonablemente.

------------------------------------------------------------------------

# 33. PRUEBAS UNITARIAS REALES

No crear tests que sólo prueben getters o mocks triviales.

Probar: - scene composition; - drawing primitives; - text clipping; -
animations; - FrameBuffer; - MatrixMapper; - capability resolver; -
power calculations; - protocol serialization; - validation; -
persistence contracts.

------------------------------------------------------------------------

# 34. PRUEBAS DEL MATRIX MAPPER

Casos obligatorios:

``` text
16x16 serpentine
32x8 progressive
2 x 16x16 → 32x16
4 x 16x16 → 32x32
rotation 90°
bottom-right origin
mirror H
mirror V
RGB
GRB
RGBW
```

Validar exactamente:

`logical X,Y → tile → physical index → physical channel/color`

------------------------------------------------------------------------

# 35. PRUEBAS DE INTEGRACIÓN

Validar flujo:

``` text
Host
→ protocol
→ controller runtime/simulator
→ scene
→ mapper
→ driver
→ expected physical pixels
```

Probar: - conexión; - capabilities; - configuración; - frame; - scene
upload; - play; - stop; - brightness; - playlist; - persistence; -
invalid payload; - reconnect; - controller restart.

------------------------------------------------------------------------

# 36. PRUEBA DE PROVISIONING

Simular/probar:

``` text
factory state
→ AP
→ configure Wi-Fi
→ persist
→ reboot
→ reconnect LAN
→ status
```

Luego: - apagar host; - perder Internet; - perder router cuando la
escena ya esté almacenada; - reiniciar controller; - comprobar
reproducción autónoma.

------------------------------------------------------------------------

# 37. WOKWI

Usar Wokwi cuando aporte integración real.

Referencia conocida: - ESP32 DevKit - Adafruit NeoPixel -
`wokwi-led-matrix` - 16x16 - serpentine - GPIO 5.

Mapping de referencia validado previamente:

``` cpp
int pixelIndex(int x, int y) {
  if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT) return -1;
  if (y % 2 == 0) return y * WIDTH + x;
  return y * WIDTH + (WIDTH - 1 - x);
}
```

No convertir este mapping concreto en regla global; debe ser sólo un
perfil/caso de prueba.

------------------------------------------------------------------------

# 38. HARDWARE-IN-LOOP

Preparar tests para cuando exista hardware:

``` text
ESP32-S3
+ real panel
+ real PSU
+ Atlas host
```

Validar: - boot; - provisioning; - colors; - first/last pixel; -
orientation; - sustained animation; - reconnect; - reboot; - stored
scene; - brightness/current limits; - thermal/power behavior; -
long-duration operation.

No marcar hardware como certificado hasta realizar prueba física.

------------------------------------------------------------------------

# 39. MATRIZ DE CERTIFICACIÓN

Mantener:

  ---------------------------------------------------------------------------------------------
  Panel/Chip   Controller   Driver   Mode    Simulator   Hardware   Firmware   Status   Notes
                                                         tested
  ------------ ------------ -------- ------- ----------- ---------- ---------- -------- -------

  ---------------------------------------------------------------------------------------------

Estados:

``` text
Planned
Implemented
Simulated
Hardware Tested
Certified
Deprecated
```

Nunca confundir `Implemented` con `Certified`.

------------------------------------------------------------------------

# 40. CRITERIO DE ACEPTACIÓN V1

El producto no está terminado sólo porque:

``` text
restore succeeds
build succeeds
tests compile
UI opens
```

Flujo mínimo obligatorio:

``` text
Create/Add device
↓
Configure display
↓
Create design
↓
Write "SE ARREGLAN COMPUTADORAS"
↓
Add animation
↓
Preview
↓
Map to target matrix
↓
Send scene
↓
Play
↓
Persist
↓
Host disconnects
↓
Controller restarts
↓
Scene is recovered
↓
Display continues autonomously
```

En simulación debe comprobarse el resultado pixel a pixel.

------------------------------------------------------------------------

# 41. UX

UI debe ser pulida y utilizable por alguien que no conoce
microcontroladores.

Modo normal: - lenguaje comercial; - seleccionar letrero; - diseñar; -
preview; - enviar.

Modo avanzado: - driver; - topology; - GPIO; - timing; - protocol; -
power.

No mostrar detalles eléctricos al cliente BASIC/SMART normal si no son
necesarios.

------------------------------------------------------------------------

# 42. MANEJO DE ERRORES

Errores deben ser accionables.

Ejemplos:

``` text
Controller offline
Unsupported capability
Scene too large
Insufficient storage
Invalid topology
Power budget exceeded
Firmware incompatible
Protocol version mismatch
Wi-Fi provisioning failed
Driver initialization failed
```

No ocultar excepciones con "Something went wrong".

------------------------------------------------------------------------

# 43. OBSERVABILIDAD

Incluir diagnóstico mínimo:

``` text
device uptime
firmware version
protocol version
Wi-Fi status
RSSI
active scene
brightness
estimated power
driver
display profile
last error
storage usage
```

Logs limitados y rotables; no llenar flash.

------------------------------------------------------------------------

# 44. RENDIMIENTO

Evitar allocations innecesarias en render/frame path.

No enviar JSON enorme frame por frame si un formato binario es más
apropiado.

Medir: - render time; - mapping time; - frame size; - transfer time; -
FPS; - memory; - storage.

Optimizar con evidencia, no prematuramente.

------------------------------------------------------------------------

# 45. OUT OF SCOPE V1

No gastar tiempo/tokens implementando ahora:

``` text
full cloud platform
native Android app
native iOS app
billing/payment gateway
AI content generation
video wall enterprise features
every MCU family
every LED chipset
complex optical ray tracing
remote Internet management
marketplace
```

Dejar interfaces/extensibilidad sólo donde tenga valor real.

------------------------------------------------------------------------

# 46. DOCUMENTACIÓN FINAL

Mantener documentación breve pero suficiente:

``` text
Architecture
Build/run
Simulator
Firmware flashing
Provisioning
Adding a driver
Adding a panel profile
Protocol
Power safety
Testing
Compatibility matrix
BOM template
```

No generar documentación redundante.

------------------------------------------------------------------------

# 47. ORDEN DE CONSTRUCCIÓN

Ejecutar en este orden salvo dependencia técnica demostrable:

``` text
1. Solution skeleton
2. Domain
3. FrameBuffer
4. MatrixMapper
5. Scene Engine
6. Driver contracts/registry
7. Capability Resolver
8. Protocol
9. Simulator
10. Persistence
11. Infrastructure/network/discovery
12. Presentation/editor
13. Native firmware architecture
14. Native API/provisioning/storage
15. Native drivers initial set
16. Power Manager
17. Playlists/schedules
18. Integration tests
19. Wokwi-compatible validation where useful
20. Security/OTA/recovery
21. End-to-end simulated product test
22. Compatibility matrix/documentation
```

Después de cada bloque importante ejecutar las pruebas pertinentes. No
esperar hasta el final para descubrir que las capas no integran.

------------------------------------------------------------------------

# 48. REGLA FINAL

La meta no es producir muchos archivos.

La meta es producir **un sistema coherente que pueda pasar de un pixel
lógico creado en el editor hasta el pixel físico correcto del letrero,
almacenar la escena y seguir ejecutándola sin PC**.

Cuando haya dos soluciones posibles: 1. preferir la más simple que
conserve extensibilidad necesaria; 2. preferir abstracción basada en
capacidades sobre `if(controller == X)`; 3. preferir pruebas observables
sobre afirmaciones; 4. preferir hardware económico cuando cumpla; 5.
preferir funcionamiento local/offline; 6. preferir
reparación/modularidad; 7. **preferir siempre ahorrar tokens sin
sacrificar corrección.**

## Definición final de éxito

AtlasLetrero V1 está listo para prototipo físico cuando el simulador y
las pruebas demuestren el flujo completo, la arquitectura permita
ESP32/ESP32-S3 y paneles iniciales sin acoplamiento, y sólo falte
sustituir el display/controlador simulado por hardware real para la
certificación HIL.

**Ejecuta. No vuelvas a describir este documento. Usa la menor salida
posible y dedica los tokens al código, verificación y pruebas.**

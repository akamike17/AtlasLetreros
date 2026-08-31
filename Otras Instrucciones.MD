ATLASLETRERO — CORRECCIÓN FINAL DIRIGIDA
NO REDESCUBRAS EL PROBLEMA. NO HAGAS UNA NUEVA AUDITORÍA GENERAL. NO ME EXPLIQUES QUÉ VAS A HACER.
IMPLEMENTA EXACTAMENTE LO SIGUIENTE SOBRE EL ESTADO ACTUAL.

OBJETIVO:
Conservar lo que ya funciona y convertir el editor actual en una herramienta de uso personal realmente pulida.
No dejes ningún bloque empezado a medias.

REGLA DE CRÉDITOS:
- TOKEN BUDGET VERY LOW.
- Lee sólo los archivos que necesites para el bloque actual.
- No narres comandos.
- No escribas planes.
- No resumas archivos completos.
- No refactorices código sano por estética.
- No agregues dependencias si puede hacerse con el stack actual.
- Termina un bloque, verifica, prueba y sólo entonces empieza el siguiente.
- Si calculas que no alcanzan los créditos para TERMINAR el siguiente bloque, NO LO EMPIECES.
- Si te detienes, deja build y pruebas verdes y di únicamente cuál bloque completo quedó pendiente.
- No commit.
- No push.

ESTADO DE PRUEBAS:
- Las pruebas .NET originales están verdes.
- Los 7 E2E originales están verdes.
- Existe tests/browser/hardening.spec.mjs.
- Total actual Playwright: 18.
- 12 pasan.
- 6 fallan.
- ESOS 6 FALLOS SON DEFECTOS CONFIRMADOS Y NO SE PUEDEN ELIMINAR NI DEBILITAR.

==================================================
BLOQUE 1 — CORREGIR LOS 6 FALLOS CONFIRMADOS
==================================================

ARCHIVO:
Controllers/SimulatorController.cs

PROBLEMA:
Los endpoints frame/play/stop/brightness dejan escapar ProtocolException y ASP.NET devuelve 500.

SOLUCIÓN EXACTA:

1. Crear un helper privado reutilizable para ejecutar acciones de protocolo.
2. Capturar ÚNICAMENTE errores esperables del cliente:
   - ProtocolException => HTTP 400 ProblemDetails.
   - ArgumentException provocada por contrato inválido => HTTP 400 sólo donde corresponda.
   - KeyNotFoundException de escena solicitada inexistente => HTTP 404 ProblemDetails.
3. NO capturar:
   - OperationCanceledException causada por cancelación real.
   - OutOfMemoryException.
   - errores internos desconocidos.
4. Aplicar el helper a:
   POST /api/frame
   POST /api/play
   POST /api/stop
   POST /api/brightness
5. Mantener el manejo existente de /api/scene.
6. Todas las respuestas de entrada inválida deben usar ProblemDetails consistente:
   status
   title/código
   detail
   type
7. Protocolo inválido debe producir 400, nunca 500.
8. Frame binario inválido debe producir 400, nunca 500.

NO arregles estos tests cambiando sus expectativas.

==================================================
FUENTES — CORREGIR LA CAUSA, NO MAQUILLARLA
==================================================

ARCHIVOS:
wwwroot/js/site.js
src/AtlasLetrero.Domain/Playback.cs
src/AtlasLetrero.Protocol/Contracts.cs
tests correspondientes

PROBLEMAS CONFIRMADOS:

A.
glyph5 actual NO es una fuente 5x7 verdadera.

Actualmente se deriva de glyph3 y contiene esta lógica defectuosa:
map((v,x)=>x===1?v+v:v)

Eso duplica una columna por ÍNDICE, no genera una fuente real.

B.
fontInfo('8x8') devuelve dimensiones 8x8 pero reutiliza glyph5.

C.
fontInfo('10x14') también reutiliza glyph5.

D.
fontDocument() sólo distingue 3x5.
Todo lo demás se serializa como base 5x7.

E.
El dominio FontProfile usa:
IReadOnlyDictionary<char, ulong>

y además prohíbe glyphWidth * glyphHeight > 64.

Por tanto una fuente REAL 10x14 (140 bits) NO cabe en el modelo actual.

NO intentes solucionar 10x14 metiendo 140 bits en JavaScript Number o ulong.
Eso sería incorrecto.

SOLUCIÓN:

1. Cambiar la representación interna del glifo a FILAS.
2. Cada fila debe ser una máscara entera.
3. Como las fuentes actuales tienen ancho máximo 10, usar ushort por fila es suficiente.

Modelo conceptual:

FontProfile:
- Id
- GlyphWidth
- GlyphHeight
- Spacing
- IReadOnlyDictionary<char, ushort[]> Rows

Validación:
- cada glifo debe tener exactamente GlyphHeight filas;
- ninguna fila puede usar bits fuera de GlyphWidth;
- width > 0;
- width <= 16;
- height > 0;
- no conservar la restricción width*height<=64.

IsPixelSet:
- obtener fila Y;
- comprobar bit X de esa fila.

4. Mantener compatibilidad de lectura con escenas antiguas si ya existe el formato ulong.
NO rompas archivos persistidos existentes.

En protocolo:
FontDocument debe poder representar filas.

Preferencia:
- nuevo campo rows
- lectura legacy de glyphs si aparece
- escritura nueva usa rows

No cambies ProtocolVersion si puedes mantener compatibilidad aditiva.

5. Definir CUATRO fuentes bitmap reales e independientes:

3x5
5x7
8x8
10x14

NO generar 8x8 y 10x14 estirando 5x7 durante render.
NO cambiar sólo OutputGlyphWidth/Height.
Cada fuente debe tener sus propios patrones bitmap.

6. Crear un único FONT_REGISTRY en frontend.
fontInfo() y fontDocument() deben consultar EL MISMO registro.

Nunca:
preview usa una fuente
y protocolo envía otra.

7. Incluir como mínimo:
A-Z
a-z mediante normalización cuando proceda
0-9
espacio
.
,
:
;
-
_
+
/
\
(
)
[
]
!
?
¿
¡
#
%
&
@
Á
É
Í
Ó
Ú
Ü
Ñ

Si falta un glifo:
usar '?' de forma determinística.

8. Los dos hardening tests:
8x8
10x14
deben pasar sin cambiar la expectativa.

CHECKPOINT:
dotnet build -c Release
dotnet test -c Release
Playwright completo
18/18 mínimo.

NO EMPIECES BLOQUE 2 SI ESTO NO ESTÁ VERDE.

==================================================
BLOQUE 2 — CORREGIR TEXTO CORTADO Y PREVIEW ENGAÑOSO
==================================================

ARCHIVOS PRINCIPALES:
wwwroot/js/site.js
Views/Home/Index.cshtml
wwwroot/css/site.css

PROBLEMA REAL ACTUAL:

textMetrics calcula el ancho completo.

Cuando alignH=center:
x = floor((project.width - textWidth)/2) + X

Si el texto es más ancho que la matriz,
X se vuelve negativo.

Luego drawText recorta contra el canvas.

Resultado:
para “SE ARREGLAN COMPUTADORAS” en 32x16 se ve sólo un fragmento central.

Eso NO es corrupción de texto.
Es overflow + centrado + clipping sin UX que lo explique.

CORREGIR ASÍ:

1. Crear una única función:
measureTextObject(object, project)

Debe devolver:
- glyphWidth
- glyphHeight
- scale
- spacing
- lineSpacing
- lines
- width
- height
- canvasWidth
- canvasHeight
- fitsWidth
- fitsHeight
- overflowLeft
- overflowRight
- overflowTop
- overflowBottom

2. Usar ESA función para:
- preview de texto;
- bounding box;
- inspector;
- ajuste automático;
- serialización;
- render.

No duplicar fórmulas.

3. En el editor mostrar permanentemente:

“Tamaño texto: 137 × 7 px”
“Matriz: 32 × 16 px”

Si no cabe:
“Este texto excede el letrero por 105 px.”

4. NO truncar silenciosamente.

5. Añadir acciones explícitas cuando no cabe:

[Marquee]
[Scroll]
[Ajustar tamaño]
[Cambiar fuente]
[Centrar]
[Permitir recorte]

6. “Ajustar tamaño”:
buscar la escala entera más grande >=1 que permita caber.
Si ni escala 1 cabe:
buscar entre las fuentes disponibles la mayor legible que quepa.
Si ninguna cabe:
proponer Marquee.

No deformar la fuente horizontalmente.

==================================================
LETRERO RÁPIDO
==================================================

Añadir modo visible:
“Letrero rápido”

Flujo exacto:

1. Campo grande de texto.
2. Matriz destino actual.
3. Fuente automática por defecto.
4. Medir mensaje.
5. Si cabe:
   - centrar H;
   - centrar V;
   - estático.
6. Si no cabe:
   - seleccionar automáticamente Marquee;
   - comenzar completamente fuera por la derecha;
   - atravesar todo el letrero;
   - terminar cuando el último píxel sale por la izquierda;
   - repetir sin salto visual.
7. Calcular una velocidad inicial razonable basada en distancia total.
8. Preview inmediatamente.
9. Permitir cambiar velocidad.
10. Botón Enviar.

“SE ARREGLAN COMPUTADORAS” en 32x16 debe funcionar perfecto sin que el usuario tenga que calcular X/Y.

==================================================
PREVIEW DE TEXTO
==================================================

PROBLEMA:
#textPreview tiene tamaño visual fijo rectangular:
width:100%;
height:90px

Eso no representa necesariamente el aspect ratio del letrero.

SOLUCIÓN:

1. El canvas lógico del preview debe usar:
width = project.width
height = project.height

2. CSS:
aspect-ratio = project.width / project.height

3. image-rendering: pixelated.

4. Escalar visualmente mediante CSS, no modificar resolución lógica.

5. Dibujar exactamente los mismos píxeles que drawText usaría en el canvas principal.

6. Añadir:
- grid opcional;
- bounding box;
- área fuera de canvas indicada visualmente cuando sea útil.

==================================================
MULTILÍNEA
==================================================

Actualmente TextElement y frontend tratan el texto esencialmente como una línea.

Implementar:

- \n crea nueva línea.
- medir cada línea.
- ancho = máximo de las líneas.
- alto = líneas * glyphHeight + lineSpacing.
- lineSpacing configurable entero.
- alineación izquierda/centro/derecha por línea.
- alineación vertical del bloque completo.

“SE ARREGLAN
COMPUTADORAS”

debe renderizarse como dos líneas reales si el usuario lo desea.

Actualizar protocolo/modelo de texto sólo si es necesario;
el string ya puede contener \n.

==================================================
BLOQUE 3 — UNIFICAR SEMÁNTICA DE SCROLL/MARQUEE
==================================================

PROBLEMA ACTUAL:

Frontend drawText con scroll utiliza una fórmula basada en:
time * project.width / project.duration

Runtime TextElement usa:
contentWidth + target.Width
dividido entre ScrollPeriod.

SON DOS SEMÁNTICAS DISTINTAS.

ESO PRODUCE PREVIEW Y DISPOSITIVO DESFASADOS.

ELIMINAR ESA DUPLICACIÓN.

Definir exactamente:

MARQUEE HORIZONTAL IZQUIERDA

distance = canvasWidth + contentWidth

progress =
(time % period) / period

originX =
canvasWidth - floor(progress * distance)

Con esto:

t=0:
el primer píxel empieza fuera a la derecha.

t=period:
todo el texto acaba de salir por la izquierda.

repeat:
vuelve a iniciar el ciclo.

Usar exactamente esa fórmula en:
- frontend;
- Domain TextElement;
- preview;
- runtime;
- golden tests.

Agregar al objeto texto:
scrollPeriodSeconds

NO inferirlo unas veces de duration y otras de speed.

Una sola fuente de verdad.

Para scroll vertical futuro:
no implementar todavía salvo que ya exista contrato completo.

==================================================
BLOQUE 4 — CORREGIR DESFASE DE OUTPUT REAL
==================================================

PROBLEMAS ACTUALES CONFIRMADOS POR CÓDIGO:

1.
Runtime renderiza hasta 30 FPS.

2.
Frontend pollDevice() sólo actualiza:
status + snapshot
cada 1000 ms.

3.
Por tanto el panel “SALIDA REAL” sólo cambia aproximadamente una vez por segundo,
aunque el runtime esté animando mucho más rápido.

Eso hace que parezca trabado/desfasado.

4.
Preview local usa performance.now() con su propio reloj.

5.
Runtime usa TimeProvider.System con otro inicio.

No son el mismo reloj y NO deben fingirse como si lo fueran.

SOLUCIÓN EXACTA:

Separar:

A. STATUS HEARTBEAT
cada 1000 ms:
- conexión;
- dispositivo;
- escena;
- estado;
- brillo;
- errores.

B. OUTPUT FRAME
cada 100 ms mientras la pestaña está visible:
- snapshot real;
- posición runtime.

Objetivo inicial:
10 FPS de visualización web del output.
NO hace falta 30 requests/s.

Implementar un fetch guard:
si la petición anterior de output no terminó,
NO lanzar otra.

Cuando document.hidden:
reducir o pausar output polling.

Al volver:
reanudar.

Mantener /api/snapshot existente para compatibilidad.

Crear endpoint adicional si hace falta:
GET /api/output

Debe devolver en la misma respuesta:

{
  protocolVersion,
  runtime: {
    isPlaying,
    activeSceneId,
    activeSceneName,
    brightness,
    positionSeconds
  },
  snapshot
}

Frontend usa /api/output para el panel real.

NO renderizar el panel real llamando raster() local.
Debe usar únicamente datos del SimulatorDevice.

==================================================
BRILLO — BUG EXACTO
==================================================

ARCHIVO:
src/AtlasLetrero.Application/Drivers.cs

Actualmente SetBrightnessAsync hace:

if (_playing)
    await RenderAsync(TimeSpan.Zero, cancellationToken);

Eso resetea visualmente la escena al frame 0 y además actualiza _position a cero.

Cambiar a:

if (_playing)
    await RenderAsync(_position, cancellationToken);

No reiniciar animación al cambiar brillo.

Agregar test:
- reproducir;
- avanzar a posición >0;
- cambiar brillo;
- position no vuelve a 0;
- frame corresponde a la misma posición con brillo nuevo.

==================================================
SINCRONIZACIÓN VISIBLE
==================================================

NO intentes sincronizar el reloj local con el hardware artificialmente.

Mostrar tres conceptos:

EDITOR
PREVIEW LOCAL
SALIDA REAL

Cuando el usuario pulsa “Enviar”:
el dispositivo inicia su propia reproducción desde t=0.

Después:
Output real muestra SU posición real.

Si el usuario quiere comparar:
añadir botón:
“Seguir salida”

Cuando está activo:
el canvas de preview usa runtime.positionSeconds recibido de /api/output
para renderizar el documento local en ese mismo timestamp.

Así pueden compararse pixel a pixel.

Agregar indicador:

✓ COINCIDE

sólo si:
- mismo SceneId/version;
- misma resolución;
- snapshot real == render local al timestamp reportado.

Si no:
⚠ DIFERENTE

NO comparar dos animaciones con relojes independientes.

==================================================
BLOQUE 5 — CAPAS REALES Y UX DE OBJETOS
==================================================

PROBLEMA:
Actualmente project.objects es una lista plana
y sceneDocument convierte cada objeto en una Layer.

Eso no es un modelo de capas real.

Migrar proyecto a:

project.layers = [
  {
    id,
    name,
    visible,
    locked,
    objects:[]
  }
]

Crear por defecto:
“Contenido”

MIGRACIÓN:
si proyecto viejo tiene project.objects:
- crear una capa “Contenido”;
- mover ahí todos los objetos conservando IDs;
- borrar project.objects sólo después de migrar correctamente;
- guardar schemaVersion.

No perder proyectos IndexedDB existentes.

==================================================
ÁRBOL DE CAPAS
==================================================

UI exacta:

CAPAS
▼ Contenido                      👁 🔓

   📝 SE ARREGLAN COMPUTADORAS
   └ Texto · 5x7 · marquee

   🖼 logo.png
   └ Imagen · 12×8

   ▭ Marco
   └ Rectángulo · 32×16

Cada capa:
- expand/collapse;
- ojo visible;
- candado;
- nombre;
- drag handle;
- menú.

Cada objeto:
- icono tipo;
- nombre;
- resumen;
- visibilidad heredada;
- selección;
- lock heredado.

Texto:
mostrar contenido, no “Texto”.

Imagen:
mostrar filename.

Stroke:
“Trazo · N puntos”.

Fill:
“Relleno · N píxeles”.

Shape:
tipo + dimensiones.

==================================================
CAPAS — COMPORTAMIENTO
==================================================

Implementar:

- crear capa;
- renombrar;
- duplicar;
- eliminar;
- mover arriba;
- mover abajo;
- drag reorder;
- visible;
- locked.

Eliminar capa no vacía:
confirmación.

Todas las operaciones:
UNDOABLE.

Locked:
- seleccionable;
- inspeccionable;
- NO movible;
- NO editable;
- NO borrable accidentalmente.

Visible false:
- no renderiza;
- no se envía como visible;
- conserva contenido.

Orden de capas:
de atrás hacia adelante de forma consistente.

NO invertir el orden entre editor y runtime.

==================================================
SELECCIÓN
==================================================

Un único selectedObjectId.

Click lista:
selecciona canvas.

Click canvas:
selecciona lista.

Objeto seleccionado:
- fila resaltada;
- bounding box;
- propiedades contextuales.

Doble click texto:
enfoca inspector de texto.

Doble click imagen:
enfoca inspector de imagen.

Delete:
elimina objeto si no está locked.

Escape:
cancela operación activa.

==================================================
BLOQUE 6 — QUITAR EL MODAL DE TEXTO COMO FLUJO PRINCIPAL
==================================================

El modal actual tapa el canvas.
Para edición repetida es mala UX.

Mantener dialog sólo si hace falta para compatibilidad temporal,
pero el flujo principal debe ser:

1. herramienta Texto;
2. click canvas;
3. crear TextElement;
4. seleccionarlo;
5. abrir automáticamente INSPECTOR contextual a la derecha;
6. enfocar textarea;
7. cambios actualizan preview en vivo;
8. Enter/Ctrl+Enter según corresponda confirma;
9. Escape revierte la edición actual si no se confirmó.

No ocultar el canvas mientras se ajusta el texto.

Inspector Texto debe incluir:

Texto
Fuente
Escala
Spacing
Line spacing
Color
X
Y
Alineación H
Alineación V
Clip
Marquee
Periodo/velocidad
Centrar H
Centrar V
Ajustar al letrero

==================================================
BLOQUE 7 — HERRAMIENTAS MENOS PRECARIAS
==================================================

PENCIL:
- brush size entero: 1,2,3,4;
- cursor muestra área;
- interpolación Bresenham entre puntos para drag rápido;
- sin huecos.

ERASER:
- mismo brush size;
- cursor distinto;
- borrar continuo.

LINE:
- preview ghost mientras arrastra;
- thickness 1..4;
- Shift restringe horizontal/vertical/45°.

RECTANGLE:
- preview durante drag;
- outline/fill;
- thickness.

CIRCLE:
- cambiar a ELLIPSE real usando bounding box.
- permitir círculo con Shift.
- outline/fill.
- preview durante drag.

FILL:
- flood fill.
- no crear operación si color destino == color origen.
- una sola entrada undo.

EYEDROPPER:
- nueva herramienta;
- click toma color del frame lógico renderizado.
- Alt+click actúa como eyedropper temporal desde Pencil.

SELECTION:
- mover objeto.
- flechas = 1 px.
- Shift+flechas = 5 px.
- Ctrl+D duplica.
- Delete elimina.
- Ctrl+C/Ctrl+V clipboard interno de objetos.

NO usar antialias.

==================================================
CANVAS
==================================================

Añadir:

- coordenadas X,Y bajo cursor;
- zoom 100/200/400/800;
- Fit;
- Ctrl+wheel zoom;
- Space+drag pan;
- middle mouse pan opcional;
- pixel snapping obligatorio.

Bounding box:
- 1px lógico;
- handles visibles;
- no formar parte del render.

Guías:
- centro horizontal;
- centro vertical;
- bordes;
- snapping opcional.

==================================================
BLOQUE 8 — INSPECTOR CONTEXTUAL
==================================================

El panel actual mezcla todo.

Separar en tabs:

DOCUMENTO
OBJETO
ANIMACIÓN
DISPOSITIVO

DOCUMENTO:
- nombre;
- resolución;
- brillo;
- duración.

OBJETO:
depende del tipo seleccionado.

ANIMACIÓN:
dejar claro si es:
“Animación de escena”

No fingir animación por objeto si Domain no la soporta.

DISPOSITIVO:
- destino;
- conexión;
- resolución;
- driver;
- SceneId;
- versión;
- output.

==================================================
BLOQUE 9 — CAMBIO DE RESOLUCIÓN
==================================================

Actualmente existen perfiles:
16x16
32x8
32x16
64x8
64x16
128x8

Al cambiar resolución:
NO cambiar inmediatamente.

Abrir diálogo con:

Mantener posición
Centrar contenido
Escalar nearest-neighbor
Recortar
Cancelar

Mostrar preview miniatura.

Texto e imagen deben seguir siendo objetos semánticos.
No rasterizar por cambiar resolución.

Si nueva resolución no coincide con dispositivo:
mostrar advertencia,
NO mentir diciendo compatible.

==================================================
BLOQUE 10 — DEVICE SELECT NO DECORATIVO
==================================================

Actualmente #deviceSelect sólo contiene:
atlas-simulator

y send() siempre usa /api/scene del mismo backend.

NO afirmar que ya existe multi-device.

Por ahora:

1. Renombrar claramente:
“Simulador local”

2. Definir interfaz frontend:
selectedDevice

con:
id
name
kind
baseUrl/route
capabilities
online

3. send() debe resolver destino desde selectedDevice.

4. Para esta V1 sólo registrar simulador local.

5. Dejar preparado añadir ESP32 después,
pero NO agregar opciones falsas.

==================================================
BLOQUE 11 — INFORMACIÓN DE SALIDA
==================================================

Panel SALIDA REAL debe mostrar:

Dispositivo
DeviceId
Resolución
Color model
Driver
Estado
Escena
SceneId
Brillo
Posición
FPS objetivo
Última actualización

No mostrar valor inventado.

Añadir botón:
Ampliar salida

Output canvas:
- pixelated;
- aspect ratio real;
- grid opcional.

==================================================
BLOQUE 12 — PROYECTOS
==================================================

Mantener IndexedDB.

Añadir schemaVersion.

Tarjeta proyecto:
- thumbnail real;
- nombre;
- resolución;
- objetos;
- última modificación;
- animación;
- estado cambios.

Autosave:
- debounce 750-1500 ms;
- NO escribir por pointermove;
- guardar al terminar operación;
- guardar al terminar edición de propiedades.

Distinguir:
Guardado
Cambios
Autoguardado
Error

beforeunload:
si dirty sin autosave confirmado,
advertir.

==================================================
BLOQUE 13 — IMÁGENES
==================================================

Mantener objeto Image editable.

Añadir preview lado a lado:

ORIGINAL
RESULTADO LED

Agregar:
- contain;
- cover;
- stretch;
- crop;
- nearest neighbor;
- transparencia;
- brightness;
- contrast;
- threshold;
- dithering Bayer simple opcional.

No usar dependencia externa.

No destruir imagen fuente.

==================================================
BLOQUE 14 — ERRORES DE UX
==================================================

Frontend responseError debe traducir códigos conocidos.

Ejemplos:

ProtocolVersionUnsupported:
“Este dispositivo usa una versión incompatible.”

CanvasMismatch:
“El diseño es 64×16 y el dispositivo es 32×16.”

InvalidScene:
“El diseño contiene datos que el dispositivo no puede reproducir.”

Offline:
“El dispositivo no está disponible.”

Mantener código técnico en detalles/log,
no como mensaje principal.

==================================================
BLOQUE 15 — PERFORMANCE
==================================================

No recrear DOM por píxel.

No persistir IndexedDB por frame.

No hacer fetch concurrente de output.

No usar setInterval que acumule requests.

Usar:
requestAnimationFrame para preview local.
throttled async loop para output remoto.

Probar:
32x16
64x16
128x8

Drag continuo debe seguir fluido.

==================================================
PRUEBAS NUEVAS OBLIGATORIAS
==================================================

Mantén TODAS las actuales.

Agregar:

1. invalid protocol:
play 400
stop 400
brightness 400
frame malformed 400

2. fuentes:
3x5 dimensiones reales
5x7 dimensiones reales
8x8 dimensiones reales
10x14 dimensiones reales
roundtrip protocolo para 10x14
glifo después de columna 64 funciona correctamente

3. texto:
texto que cabe
texto overflow
multiline
Ñ
Á
¿
marquee t=0
marquee t=period/2
marquee t=period-epsilon

4. igualdad:
frontend semantics documentada con golden vectors
Domain produce posiciones esperadas.

5. brightness:
cambiar brillo no resetea runtime position.

6. output:
positionSeconds avanza.
dos snapshots sucesivos de marquee cambian.
stop congela/limpia salida.

7. capas:
crear
rename
hide
lock
reorder
duplicate
delete
undo
redo

8. canvas:
keyboard move
pencil fast drag
eraser
line preview/final
rectangle
ellipse
fill
eyedropper
clipboard

9. resolución:
cancel
center
crop
scale

10. persistencia:
migrar proyecto viejo project.objects -> project.layers
cerrar/reabrir
no perder datos.

==================================================
PRUEBA MANUAL DE ACEPTACIÓN — OBLIGATORIA
==================================================

Usa exactamente:

SE ARREGLAN COMPUTADORAS

Matriz:
32x16

Debe ocurrir:

1. Crear Letrero rápido.
2. Escribir frase.
3. Atlas detecta que no cabe.
4. Atlas propone/activa Marquee.
5. Preview empieza fuera por la derecha.
6. Se lee TODA la frase.
7. Sale completamente por la izquierda.
8. Loop fluido.
9. Enviar.
10. Output real reproduce TODA la misma frase.
11. “Seguir salida” muestra preview local al mismo timestamp.
12. Comparación coincide.
13. Cambiar velocidad.
14. Reenviar.
15. Output cambia.
16. Guardar.
17. cerrar navegador.
18. abrir.
19. proyecto sigue completo.

Después:
- agregar logo;
- agregar marco;
- crear otra capa;
- ocultarla;
- bloquearla;
- reordenar;
- mover texto;
- undo;
- redo.

Si cualquiera de esos flujos está incómodo o roto,
NO declarar terminado.

==================================================
ORDEN OBLIGATORIO
==================================================

1. 6 hardening failures
2. sistema de fuentes real
3. texto/overflow/letrero rápido
4. scroll-marquee único
5. output/desfase/brillo
6. capas reales
7. inspector
8. herramientas/canvas
9. resolución
10. persistencia/proyectos
11. imágenes/pulido
12. tests finales

NO cambies ese orden.

Después de CADA bloque:

READ cambios
dotnet build -c Release
dotnet test -c Release
Playwright relevante

Si falla:
leer error real
corregir
repetir

Sólo entonces continuar.

==================================================
RESPUESTA
==================================================

NO escribas explicación larga.

Tras cada checkpoint responde sólo:

BLOQUE: <número/nombre>
ESTADO: TERMINADO
ARCHIVOS: <lista corta>
BUILD: PASS/FAIL
.NET TESTS: x/x
PLAYWRIGHT: x/x
SIGUIENTE: <bloque>

Si créditos están próximos a agotarse:

BLOQUE: <último terminado>
ESTADO: CHECKPOINT LIMPIO
BUILD: PASS
TESTS: PASS
NO INICIADO: <siguiente bloque>

y DETENTE.

NO DEJES NADA A MEDIAS.
AtlasLetrero V1 simulada NO está cerrada. Auditoría manual + revisión de código encontró fallos funcionales estructurales. Corrige todos en una sola fase, sin maquillar síntomas y sin declarar éxito hasta probar navegador real.

OBJETIVO
Convertir la interfaz actual de framebuffer plano en un editor usable basado en documento/proyecto, con objetos editables, preview local real, proyectos múltiples, dispositivo destino visible y pruebas E2E de navegador.

1. DOCUMENTO / PROYECTOS
- Crear modelo de proyecto/diseño con ID estable, nombre, resolución, objetos/capas, configuración de animación, brillo y metadatos.
- Nuevo.
- Abrir.
- Guardar.
- Guardar como.
- Duplicar.
- Renombrar.
- Eliminar con confirmación.
- Listado/galería de todos los diseños.
- Estado dirty/no guardado.
- No usar una única clave localStorage.
- Usar persistencia durable apropiada; no confundir MemorySceneStore con persistencia real.
- Mantener migración/lectura del atlas-design antiguo si existe.

2. CANVAS DINÁMICO
- Eliminar hardcode W=32/H=16 del frontend.
- Obtener resolución/capacidades desde dispositivo/proyecto.
- Permitir al menos los perfiles soportados por arquitectura.
- Mantener resolución lógica independiente del tamaño visual.
- Aprovechar mejor el área disponible.
- Zoom correcto sin alterar coordenadas lógicas.

3. SISTEMA DE PUNTERO
- No obtener coordenadas mediante event.target.closest('.pixel') después de setPointerCapture.
- Calcular píxel desde clientX/clientY + getBoundingClientRect.
- Implementar pointerdown/pointermove/pointerup/pointercancel/lostpointercapture robustamente.
- Liberar captura y limpiar estado siempre.
- Mouse/stylus/touch deben producir el mismo resultado.
- Añadir tests para arrastre rápido, salir/entrar canvas y pointer cancel.

4. HERRAMIENTAS
- Estado activo inequívoco.
- Imagen debe integrarse al mismo sistema de selección.
- Cursor acorde a herramienta.
- Pencil.
- Eraser.
- Line.
- Rectangle.
- Circle con comportamiento de bounding-box intuitivo.
- Fill.
- Text.
- Image.
- Selection.
- Move selection/object.
- Clear canvas.
- Undo/Redo.
- Ctrl+Z, Ctrl+Shift+Z y Ctrl+Y.
- Atajos NO deben dispararse mientras usuario escribe en input/textarea/select/contenteditable.
- Clear debe pedir confirmación y ser reversible con Undo.

5. TEXTO
- Eliminar rasterización mediante browser font "7px monospace".
- Implementar fuentes bitmap LED determinísticas 3x5, 5x7, 8x8 y al menos una fuente low-resolution mayor.
- TextElement debe seguir siendo TextElement editable.
- Editor/modal de texto con:
  texto,
  fuente,
  escala,
  color,
  spacing,
  alineación horizontal,
  alineación vertical,
  posición,
  clipping,
  scroll,
  preview.
- Centrado H/V por defecto.
- No truncar a 32 píxeles antes de colocar.
- Mostrar bounding box.
- Permitir reabrir y editar texto después de colocarlo.

6. IMÁGENES
- Mantener ImageElement editable en lugar de destruirlo inmediatamente a píxeles.
- Posición.
- Escala.
- Fit/contain/cover.
- Pixel-art nearest-neighbor.
- Recorte.
- Transparencia.
- Mover/eliminar/reeditar.
- Sólo rasterizar durante render.

7. CAPAS / OBJETOS
- Usar Scene/Layer/SceneElement reales.
- No convertir todo a PixelElement al enviar.
- Lista de capas/objetos.
- Selección.
- Orden adelante/atrás.
- Visible/oculto.
- Eliminar.
- Mover.
- Evitar entidades decorativas: sólo implementar comportamiento que UI realmente use.

8. PREVIEW LOCAL
- Play NO debe llamar send().
- Crear motor de preview local independiente del dispositivo.
- Play reproduce la escena visible usando tiempo real.
- Stop detiene y restaura estado editable.
- Pausa/reanudar si aporta valor.
- Timeline debe usar Duration real.
- Playhead real.
- Timecode real.
- Cambiar duration actualiza timeline.
- Las animaciones deben verse sin enviar nada.

9. ANIMACIONES
- Implementar/mostrar las animaciones V1 contractuales que Domain soporte:
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
  Easing donde corresponda.
- Preview y runtime deben usar exactamente la misma semántica.
- No mantener opciones UI que no funcionen.
- Añadir golden tests de frames en distintos timestamps.

10. RUNTIME DEL SIMULADOR
- Actualmente PlayAsync sólo renderiza TimeSpan.Zero.
- Implementar reloj/loop real del runtime mientras IsPlaying.
- FPS limitado por capacidades.
- CancellationToken limpio.
- Sin busy loop.
- Stop debe detener el loop inmediatamente.
- Definir semántica persistente de Stop:
  si Stop significa apagado persistente, SaveActiveSceneIdAsync(null) o persistir PlaybackState=Stopped;
  si existe Pause, diferenciarlo explícitamente.
- Reinicio debe respetar ese estado.
- Probar duración, repeat, fin de escena y reinicio.

11. SIMULADOR VISIBLE
- Crear panel/ventana claramente separada del canvas de edición.
- Consumir /api/snapshot para mostrar SALIDA REAL DEL DISPOSITIVO.
- Mostrar DeviceId.
- Nombre.
- Resolución.
- Driver/perfil.
- Estado online/offline.
- Playing/stopped.
- SceneId/nombre actual.
- Brillo.
- FPS/posición si disponible.
- Debe quedar imposible confundir editor con output.

12. DISPOSITIVOS / DESTINO
- La tarjeta destino NO puede ser estática.
- Consultar /api/status y /api/capabilities.
- Selector de dispositivos/simuladores.
- No mostrar "conectado" hardcodeado.
- Heartbeat/polling con estados online/offline/error.
- Al enviar mostrar destino elegido antes de confirmar.
- Preparar contrato para simulador + hardware real sin acoplar UI a WS2812B.
- No hardcodear 32x16, WS2812B, serpentino ni 10A en UI.

13. ENVIAR
- Enviar y Play local deben ser operaciones completamente distintas.
- Enviar debe serializar el documento semántico cuando el runtime lo soporte.
- Mantener SceneId/versionado estable.
- No crear GUID huérfano nuevo en cada envío sin gestión.
- Mostrar éxito sólo después de respuesta real.
- Mostrar qué dispositivo recibió qué versión.
- Poder volver a enviar proyecto actualizado.

14. API
- Validar nulls y rangos de DesignRequest.
- Animation desconocida => HTTP 400 controlado, nunca 500 por ArgumentException.
- Resolución contra capabilities, no constantes 32x16.
- Tamaño de pixels coherente.
- Nombre.
- Brightness.
- Duration.
- Speed.
- ProtocolVersion.
- Respuestas ProblemDetails consistentes.
- Frontend debe manejar JSON, ProblemDetails y fallos de red.
- Stop/Play/Send deben revisar response.ok.

15. PERSISTENCIA
- La app web no debe depender únicamente de MemorySceneStore.
- Conectar store durable para escenas/proyectos locales.
- Reiniciar PROCESO COMPLETO y comprobar recuperación, no sólo SimulatorDevice.RestartAsync reutilizando el mismo store.
- Probar crash/restart.
- Escritura atómica.
- No perder último proyecto válido por archivo parcial.

16. BRILLO
- Preview de brillo debe afectar emisión de LEDs, no opacity de toda la cuadrícula/UI.
- Runtime y preview deben producir resultado coherente.
- Brillo 0 debe tener semántica definida si se permite.

17. ENERGÍA
- Eliminar "10 A disponibles" hardcodeado.
- Usar PowerProfile/configuración real.
- Cálculo por tecnología/color model.
- RGBW cuando aplique.
- Mostrar estimación, no presentarla como medición.
- Integrar límites de potencia antes de hardware físico.

18. UNDO/REDO
- Historial del DOCUMENTO completo:
  píxeles,
  objetos,
  texto,
  imágenes,
  capas,
  posición,
  animación,
  duración,
  velocidad,
  brillo cuando corresponda.
- Cancelar una operación no debe insertar checkpoint falso.
- Limitar memoria razonablemente.

19. UX
- No usar prompt() para texto.
- Modales/paneles accesibles.
- Feedback de herramienta activa.
- Tooltips/shortcuts.
- Empty state.
- Error state.
- Loading state.
- Offline state.
- Unsaved state.
- Confirmaciones sólo para operaciones destructivas.
- Responsive mínimo para escritorio.

20. TESTS E2E REALES
Agregar Playwright o equivalente y arrancar ASP.NET real.

Casos mínimos obligatorios:
- navegador abre editor;
- seleccionar cada herramienta;
- dibujo drag continuo;
- borrar;
- línea;
- rectángulo;
- círculo;
- fill;
- pointer capture/cancel;
- texto 3x5 centrado;
- texto 5x7 centrado;
- texto largo;
- editar texto existente;
- imagen;
- selección/mover;
- clear + undo;
- redo;
- guardar diseño A;
- guardar diseño B;
- listar ambos;
- cerrar/reabrir navegador y recuperarlos;
- abrir A;
- borrar B;
- dirty indicator;
- Play local sin llamadas a /api/design;
- Blink visible;
- Scroll visible;
- Pulse visible;
- Stop local;
- duración/timeline;
- seleccionar simulador;
- enviar;
- comprobar /api/snapshot pixel por pixel;
- detener dispositivo;
- offline handling;
- reiniciar proceso ASP.NET;
- recuperar proyecto;
- comprobar estado stopped/playing según semántica definida.

21. NO LLAMAR E2E A TESTS QUE INSTANCIAN CLASES DIRECTAMENTE
Mantenerlos como integration/runtime tests, pero distinguir:
Unit
Integration
Runtime
Browser E2E
HIL

22. CRITERIO DE CIERRE
No cerrar V1 porque dotnet test pase.
Debe pasar:
- build Release 0 errores/0 warnings;
- unit/integration existentes;
- browser E2E;
- prueba manual real desde navegador;
- preview local visible;
- proyectos múltiples;
- simulador output visible;
- animación corriendo;
- reinicio completo de proceso;
- documentación ajustada a lo realmente probado.

READ -> EDIT/WRITE -> READ/VERIFY -> BUILD/TEST -> CORRECT -> RETEST.

No preguntes decisiones ya resueltas.
No te detengas en diagnóstico si la corrección es segura.
No maquilles controles ni agregues botones sin comportamiento real.
No declares funcionalidad por existencia de clases.
No declares V1 cerrada hasta demostrar cada flujo desde navegador.
# Arquitectura

```text
Editor web / host .NET
  → Scene Engine + FrameBuffer
  → protocolo JSON para escenas / ATLF binario para frames
  → Controller Runtime o SimulatorDevice
  → MatrixMapper
  → IDisplayDriver
  → transporte RMT / SPI / HUB75 DMA
  → panel
```

El dominio no conoce GPIO, RMT, DMA ni SDKs. `AtlasLetrero.Domain` contiene escenas, gráficos, mapping, playlists, horarios, capacidades y energía. `Application` coordina drivers y reproducción. `Protocol` versiona y limita payloads. `Infrastructure` aporta archivos atómicos, MySQL y red. `Simulator` usa el mismo motor, mapping y protocolo. `AtlasLetrero.Native` implementa el runtime portable C++17.

Atlas Native separa interfaces de plataforma —red, almacenamiento, reloj, entropía, credenciales, OTA y recovery— de la lógica comprobable. ESP-IDF debe implementar esas interfaces y registrar transportes físicos sin introducir condiciones por modelo dentro del dominio.

El firmware y el contenido son independientes: cambiar el anuncio no requiere reflashear. Al arrancar, el controlador carga configuración y escena/playlist activa; si el host desaparece, continúa localmente.

## Persistencia

Las escenas y el identificador activo se escriben antes de iniciar reproducción. El host dispone de almacenamiento atómico y MySQL transaccional. En firmware, la implementación de plataforma debe usar NVS/partición de datos con escritura atómica, límites y backup; las interfaces ya cubren configuración, red, escenas, playlists y horarios.

## Protocolo

- Versión actual: `1`.
- Frames: encabezado `ATLF`, dimensiones, modelo RGB/RGBW, longitud declarada y canales compactos.
- Escenas: documento JSON limitado a 2 MiB, 128 capas y 10,000 elementos.
- API local: status, capabilities, config, frame, scene, play, stop, brightness, provisioning y OTA.
- Payload incorrecto, sobredimensionado o de otra versión se rechaza antes del render.

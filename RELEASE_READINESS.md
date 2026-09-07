# Release readiness — Atlas Letrero 0.1.0

La versión 0.1.0 es una beta local para Windows. Se publica como paquete portable mediante `tools/publish.ps1` y requiere el runtime de .NET 8.

Antes de vender una edición comercial deben completarse: prueba con ESP32 y matriz LED real, firmware firmado y versionado, instalador firmado, actualización/migración de proyectos, pruebas de carga y una política de soporte.

Comprobaciones de cada entrega:

1. Ejecutar `npm test`.
2. Ejecutar `tools/smoke.ps1` en Windows con Chromium disponible.
3. Ejecutar `tools/verify-assets.ps1`.
4. Ejecutar `tools/publish.ps1` y probar el ZIP en una cuenta Windows sin datos previos.
5. Verificar que `/api/health` informa versión y que la aplicación escucha sólo en loopback.

El firmware físico no se declara listo hasta completar una prueba HIL documentada.

## Estado de la entrega

El CI ejecuta ahora el smoke E2E completo. El envío físico está conectado al servicio Serial y sólo habilita el destino después de un handshake AtlasLED válido; valida capacidades y conserva el principio validar → preparar → enviar → verificar → activar. Sin ESP32 real, el estado correcto es `BLOCKED BY HARDWARE`.

La versión sigue siendo beta Windows. La prueba E2E, el build Release y la verificación de assets deben ejecutarse antes de cada entrega.

# Operación, seguridad y energía

## Provisioning

Un equipo sin credenciales inicia el AP local `AtlasLED-XXXX`. El portal guarda SSID/clave en una implementación de `INetworkCredentialVault`, intenta estación y vuelve al AP si falla. Un equipo configurado intenta LAN al arrancar; perder Internet no detiene contenido almacenado. Factory reset elimina las credenciales y regresa al AP.

La implementación ESP debe conectar la bóveda a NVS cifrada. Nunca registrar contraseñas ni incluirlas en repositorio, firmware o respuestas API.

## Seguridad y OTA

- Identidad y token de emparejamiento se generan con entropía de plataforma.
- Los comandos mutables usan token local; intentos repetidos producen bloqueo temporal.
- La OTA limita tamaño, exige firma mediante el verificador de plataforma y consulta su contador anti-rollback.
- La imagen se escribe en una partición inactiva, se activa al reiniciar y sólo se confirma tras un arranque saludable; de lo contrario se revierte.
- Tres fallos de arranque restauran backup y activan modo seguro.

En ESP32/ESP32-S3 deben habilitarse las funciones oficiales de secure boot, flash encryption, OTA con dos slots, anti-rollback y NVS encryption según el modelo de producción. Las pruebas portables validan el flujo, no sustituyen esas garantías.

## Alimentación

El `PowerManager` considera voltaje, corriente disponible, reserva del controlador, contenido RGB/RGBW y brillo máximo. Reduce el brillo del frame para permanecer dentro del presupuesto y reporta corriente/potencia estimadas.

Referencia conservadora para WS2812B: hasta aproximadamente 60 mA por píxel en blanco completo. Una matriz 16×16 puede alcanzar unos 15.36 A teóricos; una fuente 5 V/10 A requiere límite de potencia apropiado.

El software no reemplaza fusible, calibre de cable, distribución/inyección de energía, tierra común, ventilación ni protecciones. Nunca conectar 12 V directamente a un panel o entrada de 5 V. Para 12 V usar un buck físico realmente dimensionado para la carga continua.

## BOM mínimo de prototipo

| Elemento | Criterio |
|---|---|
| ESP32-S3 con PSRAM | Desarrollo y cargas grandes; ESP32 económico si cumple capacidades |
| Panel WS2812B 16×16 | Perfil inicial de referencia, 5 V, GRB, serpentino |
| Fuente 5 V/10 A/50 W | Sólo con límite de potencia y margen verificado |
| Fusible, borneras y cable | Dimensionados para corriente real y longitud |
| Level shifter 3.3→5 V | Recomendado según panel/cableado |
| Carcasa y difusor | Sin impedir ventilación ni mantenimiento |

Precios y proveedores no se fijan en el dominio; deben verificarse al comprar.

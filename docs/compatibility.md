# Compatibilidad y certificación

Estados: `Planned`, `Implemented`, `Simulated`, `Hardware Tested`, `Certified`, `Deprecated`.

| Panel/chip | Controlador objetivo | Driver | Modo | Simulador | Hardware | Firmware | Estado | Notas |
|---|---|---|---|---|---|---|---|---|
| WS281x / WS2812B | ESP32 / ESP32-S3 | `generic.addressable` | RGB, GRB 800 kHz | Perfil 16×16 validado | No | Core portable | Simulated | Mapping serpentino y bytes probados; GPIO 5 preparado para Wokwi |
| SK6812 RGBW | ESP32 / ESP32-S3 | `generic.addressable` | RGBW, GRBW 800 kHz | Contrato probado | No | Core portable | Implemented | Falta transporte ESP-IDF y panel real |
| APA102 | ESP32 / ESP32-S3 | `generic.clocked` | RGB, DATA+CLOCK | Contrato probado | No | Core portable | Implemented | Framing y brillo global probados |
| SK9822 | ESP32 / ESP32-S3 | `generic.clocked` | RGB, DATA+CLOCK | No específico | No | Perfil definido | Implemented | Requiere validación de timing físico |
| HUB75 | ESP32-S3 + PSRAM | `hub75` | RGB paralelo/DMA | Contrato probado | No | Core portable | Implemented | Falta backend DMA y panel real |
| MAX7219/MAX7221 | Por definir | `max7219` | Monocromo SPI | No | No | No | Planned | Arquitectura preparada; fuera del conjunto V1 inicial |
| HT16K33 | Por definir | futuro | Monocromo I²C | No | No | No | Planned | Sin implementación V1 |

## Controladores

| Plataforma | Estado | Alcance comprobado |
|---|---|---|
| Host/simulador .NET 8 | Simulated | Flujo completo, persistencia y resultado píxel por píxel |
| Core C++17 portable | Implemented | Runtime, API, drivers, energía, scheduler, seguridad, OTA/recovery |
| ESP32 | Planned | Objetivo inicial; falta composición ESP-IDF, flash y HIL |
| ESP32-S3/PSRAM | Planned | Objetivo principal de desarrollo; falta composición ESP-IDF, flash y HIL |
| RP2040/RP2350, STM32, nRF52, Raspberry Pi | Planned | Sólo extensión arquitectónica; no V1 |

Wokwi contiene diagrama y contrato 16×16, pero no se ejecutó un ELF ESP-IDF porque `app_main` físico sigue pendiente. Ninguna combinación está `Hardware Tested` ni `Certified`.

## Criterio para subir de estado

- `Implemented`: compila y tiene pruebas del contrato.
- `Simulated`: flujo observable completo en simulador representativo.
- `Hardware Tested`: boot, colores, esquinas, mapping, animación sostenida, red, reinicio, persistencia, potencia y temperatura comprobados en equipo real.
- `Certified`: HIL repetible, límites documentados y versión exacta de firmware/hardware registrada.

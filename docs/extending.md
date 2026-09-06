# Agregar drivers y perfiles

## Driver nuevo

1. Implementar `IDisplayDriver` en .NET o C++ conservando `initialize`, brillo, render, clear y test.
2. Declarar capacidades físicas reales: píxeles, salidas, RGB/RGBW y brillo.
3. Mantener el mapping fuera del SDK; usar `MatrixMapper` antes de emitir canales físicos.
4. Encapsular el SDK en un transporte (`IAddressableTransport`, `IClockedTransport` o `IHub75Transport`) o crear una interfaz equivalente cuando la familia realmente lo necesite.
5. Registrar la factory por identificador estable y añadir pruebas de bytes, orden de canales, límites, clear y fallo de inicialización.

No duplicar un driver para cada producto comercial. WS2812B es `generic.addressable` + 800 kHz + GRB + reset; SK6812 RGBW cambia perfil/modelo. APA102 y SK9822 usan `generic.clocked`. Un driver especializado sólo corresponde cuando el protocolo lo exige, como HUB75 DMA.

## Perfil de panel

Un perfil conocido define familia, GPIO/timing, dimensiones, RGB/RGBW, orden de canales y topología. Los paneles pasivos no se autodetectan: el usuario selecciona un perfil o configura origen, recorrido, serpentino/progresivo, rotación, espejo y tiles.

Cada perfil debe incluir un caso observable:

```text
logical X,Y → tile → physical index → channel bytes
```

Agregar soporte de librería equivale a `Implemented`; sólo una prueba física documentada permite `Hardware Tested`, y únicamente el proceso de aceptación completo permite `Certified`.

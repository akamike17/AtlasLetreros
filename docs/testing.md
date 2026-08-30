# Pruebas

## .NET

```powershell
dotnet test .\AtlasLetreros.slnx --configuration Release
```

Las suites cubren dominio, protocolo, aplicación, infraestructura, simulador e integración. La prueba end-to-end crea “SE ARREGLAN COMPUTADORAS”, agrega marquesina, serializa, carga, reproduce, compara 1,024 píxeles/3,072 canales, desconecta el host, reinicia y vuelve a comparar.

## Firmware portable

Configurar CMake con un compilador C++17 y ejecutar:

```powershell
cmake -S .\firmware\AtlasLetrero.Native -B .\firmware\AtlasLetrero.Native\build
cmake --build .\firmware\AtlasLetrero.Native\build
ctest --test-dir .\firmware\AtlasLetrero.Native\build --output-on-failure
```

Suites: core nativo, integración API/runtime, perfil Wokwi y seguridad/OTA. La ejecución portable no acredita timing eléctrico ni hardware.

## HIL pendiente

Con ESP32-S3, panel y fuente reales registrar: revisión de placa/panel, firmware, driver/perfil, alimentación, primer/último píxel, colores, orientación, FPS sostenido, reconexión, reboot, escena persistida, límites de corriente y comportamiento térmico. No cambiar la matriz de compatibilidad a `Hardware Tested` sin esa evidencia.

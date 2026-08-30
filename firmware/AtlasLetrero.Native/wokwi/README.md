# Perfil de validación Wokwi

Perfil de referencia: ESP32 DevKit, matriz WS2812 de 16×16, cableado serpentino, GRB a 800 kHz y datos por GPIO 5.

El contrato de mapping y serialización se ejecuta localmente como `wokwi_profile_tests`. El diagrama queda preparado para cargar el artefacto ESP-IDF indicado en `wokwi.toml` cuando exista la composición física de `app_main` y se genere `esp-idf/build/flasher_args.json`.

Estado actual: **perfil simulado por contrato; ejecución dentro de Wokwi pendiente**. No representa prueba ni certificación de hardware.

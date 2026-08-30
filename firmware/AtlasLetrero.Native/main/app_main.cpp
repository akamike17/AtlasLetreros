#include "atlas/controller_runtime.hpp"

// ESP-IDF composition belongs here. Concrete NVS, Wi-Fi and display adapters
// are registered by target profile; the portable runtime never includes ESP headers.
extern "C" void app_main() {
    // Block 14 supplies the ESP-IDF adapters and starts ControllerRuntime.
}

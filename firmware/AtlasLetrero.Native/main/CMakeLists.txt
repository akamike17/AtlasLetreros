idf_component_register(
  SRCS "../src/matrix_mapper.cpp" "../src/driver_registry.cpp" "../src/controller_runtime.cpp" "../src/provisioning.cpp" "../src/local_api.cpp" "../src/native_drivers.cpp" "../src/power_manager.cpp" "../src/playlist_scheduler.cpp" "../src/security.cpp" "../src/ota_recovery.cpp" "app_main.cpp"
  INCLUDE_DIRS "../include"
  REQUIRES esp_event nvs_flash)
target_compile_features(${COMPONENT_LIB} PUBLIC cxx_std_17)

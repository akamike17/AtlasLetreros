cmake_minimum_required(VERSION 3.20)
project(AtlasLetreroNative VERSION 0.1.0 LANGUAGES CXX)

add_library(atlas_native STATIC
    src/matrix_mapper.cpp
    src/driver_registry.cpp
    src/controller_runtime.cpp
    src/provisioning.cpp
    src/local_api.cpp
    src/native_drivers.cpp)
target_sources(atlas_native PRIVATE src/power_manager.cpp)
target_sources(atlas_native PRIVATE src/playlist_scheduler.cpp)
target_sources(atlas_native PRIVATE src/security.cpp src/ota_recovery.cpp)
target_include_directories(atlas_native PUBLIC include)
target_compile_features(atlas_native PUBLIC cxx_std_17)
if(MSVC)
  target_compile_options(atlas_native PRIVATE /W4 /WX /permissive-)
else()
  target_compile_options(atlas_native PRIVATE -Wall -Wextra -Wpedantic -Werror)
endif()

enable_testing()
add_executable(atlas_native_tests tests/native_tests.cpp)
target_link_libraries(atlas_native_tests PRIVATE atlas_native)
add_test(NAME atlas_native_tests COMMAND atlas_native_tests)
add_executable(atlas_native_integration_tests tests/native_integration_tests.cpp)
target_link_libraries(atlas_native_integration_tests PRIVATE atlas_native)
add_test(NAME atlas_native_integration_tests COMMAND atlas_native_integration_tests)
add_executable(wokwi_profile_tests tests/wokwi_profile_tests.cpp)
target_link_libraries(wokwi_profile_tests PRIVATE atlas_native)
add_test(NAME wokwi_profile_tests COMMAND wokwi_profile_tests)
add_executable(security_ota_tests tests/security_ota_tests.cpp)
target_link_libraries(security_ota_tests PRIVATE atlas_native)
add_test(NAME security_ota_tests COMMAND security_ota_tests)

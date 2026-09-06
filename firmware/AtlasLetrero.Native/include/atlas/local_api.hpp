#pragma once
#include "controller_runtime.hpp"
#include "provisioning.hpp"
#include "security.hpp"
#include "ota_recovery.hpp"

namespace atlas {
enum class HttpMethod : std::uint8_t { get, post };
struct ApiRequest final { HttpMethod method; std::string path; std::vector<std::uint8_t> body; std::string scene_id; std::string authentication_token; std::vector<std::uint8_t> signature; std::string firmware_version; };
struct ApiResponse final { std::uint16_t status; std::string content_type; std::vector<std::uint8_t> body; };
class LocalApi final {
public:
    static constexpr std::size_t maximum_frame_bytes=4*maximum_pixels+14;
    static constexpr std::size_t maximum_scene_bytes=2*1024*1024;
    LocalApi(ControllerRuntime& runtime,IConfigurationStore& storage,ProvisioningManager& provisioning,DisplayConfiguration configuration,LocalAuthenticator* authenticator=nullptr,OtaManager* ota=nullptr);
    [[nodiscard]] ApiResponse handle(const ApiRequest& request);
private:
    ApiResponse json(std::uint16_t status,std::string value) const;
    ApiResponse error(std::uint16_t status,std::string_view code,std::string_view message) const;
    ControllerRuntime& runtime_; IConfigurationStore& storage_; ProvisioningManager& provisioning_; DisplayConfiguration configuration_;LocalAuthenticator* authenticator_{};OtaManager* ota_{};
};
} // namespace atlas

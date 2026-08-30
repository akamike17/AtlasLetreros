#pragma once
#include "platform.hpp"

namespace atlas {
enum class ProvisioningState : std::uint8_t { factory, access_point, connecting, connected, failed };
struct ProvisioningResult final { bool accepted{}; bool reboot_required{}; std::string error; };
class ProvisioningManager final {
public:
    ProvisioningManager(IConfigurationStore& storage, INetworkStack& network, ILogger& logger, INetworkCredentialVault* credential_vault=nullptr);
    bool boot(); ProvisioningResult submit(NetworkCredentials credentials); bool factory_reset();
    [[nodiscard]] ProvisioningState state() const noexcept { return state_; }
    [[nodiscard]] static bool valid_host_name(std::string_view value) noexcept;
private:
    IConfigurationStore& storage_; INetworkStack& network_; ILogger& logger_; INetworkCredentialVault* credential_vault_{}; ProvisioningState state_{ProvisioningState::factory};
};
} // namespace atlas

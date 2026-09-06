#pragma once
#include "display_driver.hpp"
#include "platform.hpp"
#include "power_manager.hpp"

namespace atlas {
class ControllerRuntime final {
public:
    ControllerRuntime(IDisplayDriver& driver, IConfigurationStore& storage, INetworkStack& network, ILogger& logger, const PowerManager* power_manager=nullptr);
    bool boot();
    bool receive_frame(FrameView frame);
    bool store_and_play(std::string_view scene_id, FrameView frame);
    bool play_stored(std::string_view scene_id);
    void stop() noexcept;
    bool set_brightness(std::uint8_t brightness);
    void set_active_program(std::string_view playlist_id,std::string_view schedule_id);
    [[nodiscard]] const RuntimeStatus& status() const noexcept { return status_; }
private:
    bool compatible(FrameView frame) const noexcept;
    bool render(FrameView frame);
    IDisplayDriver& driver_; IConfigurationStore& storage_; INetworkStack& network_; ILogger& logger_;
    DisplayConfiguration configuration_{}; RuntimeStatus status_{}; const PowerManager* power_manager_{};
};
} // namespace atlas

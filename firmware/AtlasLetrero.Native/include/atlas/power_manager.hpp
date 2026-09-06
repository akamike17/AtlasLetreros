#pragma once
#include "types.hpp"

namespace atlas {
struct PowerProfile final {
    std::uint16_t supply_millivolts{5'000};
    std::uint32_t available_milliamps{10'000};
    std::uint32_t controller_reserve_milliamps{500};
    std::uint8_t maximum_brightness{255};
    std::uint8_t milliamps_per_channel{20};
};

struct PowerDecision final {
    std::uint8_t requested_brightness{};
    std::uint8_t applied_brightness{};
    std::uint32_t estimated_milliamps{};
    std::uint32_t budget_milliamps{};
    std::uint32_t estimated_milliwatts{};
    bool limited{};
};

class PowerManager final {
public:
    explicit PowerManager(PowerProfile profile);
    [[nodiscard]] PowerDecision evaluate(FrameView frame, std::uint8_t display_brightness) const noexcept;
    [[nodiscard]] const PowerProfile& profile() const noexcept { return profile_; }
private:
    PowerProfile profile_;
};
} // namespace atlas

#pragma once
#include "display_driver.hpp"
#include "driver_transports.hpp"
#include "matrix_mapper.hpp"
#include <memory>

namespace atlas {
struct AddressableProfile final {
    std::string_view id; std::uint32_t frequency_hz{800'000}; std::uint16_t reset_microseconds{80};
    ChannelOrder order{ChannelOrder::grb}; ColorModel color_model{ColorModel::rgb};
};
inline const AddressableProfile ws2812b_profile{"ws2812b",800'000,80,ChannelOrder::grb,ColorModel::rgb};
inline const AddressableProfile sk6812_rgbw_profile{"sk6812.rgbw",800'000,80,ChannelOrder::grbw,ColorModel::rgbw};

class AddressableDriver final : public IDisplayDriver {
public:
    AddressableDriver(IAddressableTransport& transport, AddressableProfile profile);
    std::string_view id() const noexcept override; DriverCapabilities capabilities() const noexcept override;
    bool initialize(const DisplayConfiguration&) override; bool set_brightness(std::uint8_t) override;
    bool render(FrameView) override; void clear() noexcept override; bool test() override;
private:
    IAddressableTransport& transport_; AddressableProfile profile_; DisplayConfiguration configuration_{}; std::unique_ptr<MatrixMapper> mapper_; std::uint8_t brightness_{255}; bool initialized_{};
};

struct ClockedProfile final { std::string_view id; std::uint32_t frequency_hz{8'000'000}; };
inline const ClockedProfile apa102_profile{"apa102",8'000'000};
inline const ClockedProfile sk9822_profile{"sk9822",8'000'000};

class ClockedDriver final : public IDisplayDriver {
public:
    ClockedDriver(IClockedTransport& transport, ClockedProfile profile);
    std::string_view id() const noexcept override; DriverCapabilities capabilities() const noexcept override;
    bool initialize(const DisplayConfiguration&) override; bool set_brightness(std::uint8_t) override;
    bool render(FrameView) override; void clear() noexcept override; bool test() override;
private:
    IClockedTransport& transport_; ClockedProfile profile_; DisplayConfiguration configuration_{}; std::unique_ptr<MatrixMapper> mapper_; std::uint8_t brightness_{255}; bool initialized_{};
};

class Hub75Driver final : public IDisplayDriver {
public:
    explicit Hub75Driver(IHub75Transport& transport);
    std::string_view id() const noexcept override; DriverCapabilities capabilities() const noexcept override;
    bool initialize(const DisplayConfiguration&) override; bool set_brightness(std::uint8_t) override;
    bool render(FrameView) override; void clear() noexcept override; bool test() override;
private:
    IHub75Transport& transport_; DisplayConfiguration configuration_{}; std::unique_ptr<MatrixMapper> mapper_; std::uint8_t brightness_{255}; bool initialized_{};
};
} // namespace atlas

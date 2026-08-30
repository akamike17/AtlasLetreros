#pragma once
#include "types.hpp"
#include <memory>
#include <functional>
#include <string_view>

namespace atlas {
struct DriverCapabilities final { std::size_t maximum_pixels{}; std::uint8_t maximum_outputs{}; bool rgb{}, rgbw{}, brightness{}; };

class IDisplayDriver {
public:
    virtual ~IDisplayDriver() = default;
    [[nodiscard]] virtual std::string_view id() const noexcept = 0;
    [[nodiscard]] virtual DriverCapabilities capabilities() const noexcept = 0;
    virtual bool initialize(const DisplayConfiguration& configuration) = 0;
    virtual bool set_brightness(std::uint8_t brightness) = 0;
    virtual bool render(FrameView frame) = 0;
    virtual void clear() noexcept = 0;
    virtual bool test() = 0;
};
using DriverFactory = std::function<std::unique_ptr<IDisplayDriver>()>;
} // namespace atlas

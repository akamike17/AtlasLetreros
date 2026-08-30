#pragma once
#include "types.hpp"
#include <cstdint>
#include <vector>

namespace atlas {
class IAddressableTransport {
public:
    virtual ~IAddressableTransport() = default;
    virtual bool configure(std::uint32_t frequency_hz, std::uint16_t reset_microseconds) = 0;
    virtual bool transmit(const std::vector<std::uint8_t>& bytes) = 0;
};

class IClockedTransport {
public:
    virtual ~IClockedTransport() = default;
    virtual bool configure(std::uint32_t frequency_hz) = 0;
    virtual bool transmit(const std::vector<std::uint8_t>& bytes) = 0;
};

class IHub75Transport {
public:
    virtual ~IHub75Transport() = default;
    virtual bool configure(std::uint16_t width, std::uint16_t height) = 0;
    virtual bool present(const std::vector<Pixel>& pixels, std::uint8_t brightness) = 0;
};
} // namespace atlas

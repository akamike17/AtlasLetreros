#pragma once
#include "types.hpp"
#include <vector>

namespace atlas {
struct PhysicalAddress final { std::size_t tile_index{}, physical_index{}, absolute_index{}; };

class MatrixMapper final {
public:
    explicit MatrixMapper(DisplayConfiguration configuration);
    [[nodiscard]] PhysicalAddress map(std::uint16_t x, std::uint16_t y) const;
    [[nodiscard]] std::vector<std::uint8_t> encode(FrameView frame) const;
private:
    DisplayConfiguration configuration_; std::vector<std::size_t> offsets_;
};
} // namespace atlas

#pragma once

#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

namespace atlas {

inline constexpr std::uint16_t protocol_version = 1;
inline constexpr std::size_t maximum_pixels = 1'048'576;

enum class ColorModel : std::uint8_t { rgb = 0, rgbw = 1 };
enum class Origin : std::uint8_t { top_left, top_right, bottom_left, bottom_right };
enum class Traversal : std::uint8_t { row, column };
enum class Layout : std::uint8_t { progressive, serpentine };
enum class Rotation : std::uint8_t { none, clockwise_90, clockwise_180, clockwise_270 };
enum class Mirror : std::uint8_t { none, horizontal, vertical, both };
enum class ChannelOrder : std::uint8_t { rgb, rbg, grb, gbr, brg, bgr, rgbw, grbw, brgw, wrgb };

struct Pixel final { std::uint8_t r{}, g{}, b{}, w{}; bool operator==(const Pixel& other) const noexcept { return r==other.r&&g==other.g&&b==other.b&&w==other.w; } };
struct Tile final {
    std::uint16_t logical_x{}, logical_y{}, width{}, height{};
    Origin origin{Origin::top_left}; Traversal traversal{Traversal::row}; Layout layout{Layout::progressive};
    Rotation rotation{Rotation::none}; Mirror mirror{Mirror::none}; std::uint16_t chain_index{};
    [[nodiscard]] constexpr std::size_t pixel_count() const noexcept { return static_cast<std::size_t>(width) * height; }
    [[nodiscard]] constexpr std::uint16_t physical_width() const noexcept {
        return rotation == Rotation::clockwise_90 || rotation == Rotation::clockwise_270 ? height : width;
    }
    [[nodiscard]] constexpr std::uint16_t physical_height() const noexcept {
        return rotation == Rotation::clockwise_90 || rotation == Rotation::clockwise_270 ? width : height;
    }
};

struct DisplayConfiguration final {
    std::uint16_t width{}, height{}; ColorModel color_model{ColorModel::rgb}; ChannelOrder channel_order{ChannelOrder::rgb};
    std::uint8_t brightness{255}; std::vector<Tile> tiles;
};

struct FrameView final {
    std::uint16_t width{}, height{}; ColorModel color_model{ColorModel::rgb}; std::uint8_t brightness{255};
    const std::vector<Pixel>& pixels;
};

struct DeviceIdentity final { std::string id; std::string serial_number; std::string host_name; };
struct PlaylistItem final { std::string scene_id; std::uint32_t duration_milliseconds{}; };
struct Playlist final { std::string id; std::vector<PlaylistItem> items; bool repeat{true}; };
struct Schedule final { std::string id; std::string playlist_id; std::uint8_t weekdays_mask{0x7f}; std::uint16_t start_minute{},end_minute{1440}; std::uint8_t priority{}; bool enabled{true}; };
struct RuntimeStatus final { bool configured{}, playing{}, power_limited{}; std::uint8_t applied_frame_brightness{255}; std::uint32_t estimated_current_milliamps{},estimated_power_milliwatts{}; std::uint64_t rendered_frames{}; std::string active_scene_id; std::string active_playlist_id; std::string active_schedule_id; std::string last_error; };

} // namespace atlas

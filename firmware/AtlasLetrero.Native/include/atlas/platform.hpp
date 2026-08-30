#pragma once
#include "types.hpp"
#include <optional>
#include <string_view>
#include <vector>

namespace atlas {
struct NetworkCredentials final { std::string ssid; std::string password; };
class INetworkCredentialVault { public: virtual ~INetworkCredentialVault()=default;virtual std::optional<NetworkCredentials> load()=0;virtual bool save(const NetworkCredentials&)=0;virtual bool clear()=0; };
class IConfigurationStore {
public:
    virtual ~IConfigurationStore() = default;
    [[nodiscard]] virtual std::optional<DisplayConfiguration> load_display() = 0;
    virtual bool save_display(const DisplayConfiguration&) = 0;
    [[nodiscard]] virtual std::optional<std::string> load_active_scene_id() = 0;
    virtual bool save_active_scene_id(std::string_view id) = 0;
    [[nodiscard]] virtual std::optional<std::vector<Pixel>> load_scene_frame(std::string_view id) = 0;
    virtual bool save_scene_frame(std::string_view id, const std::vector<Pixel>& pixels) = 0;
    [[nodiscard]] virtual std::optional<NetworkCredentials> load_network() = 0;
    virtual bool save_network(const NetworkCredentials&) = 0;
    virtual bool clear_network() = 0;
    virtual bool save_scene_blob(std::string_view id, const std::vector<std::uint8_t>& payload) = 0;
    [[nodiscard]] virtual std::optional<std::vector<std::uint8_t>> load_scene_blob(std::string_view id) = 0;
    [[nodiscard]] virtual std::vector<Playlist> load_playlists() = 0;
    virtual bool save_playlist(const Playlist&) = 0;
    [[nodiscard]] virtual std::vector<Schedule> load_schedules() = 0;
    virtual bool save_schedule(const Schedule&) = 0;
    [[nodiscard]] virtual std::optional<std::string> load_active_playlist_id() = 0;
    virtual bool save_active_playlist_id(std::string_view id) = 0;
};
class INetworkStack {
public:
    virtual ~INetworkStack() = default; virtual bool configure_station(const NetworkCredentials&) = 0;
    virtual bool start_station() = 0; virtual bool start_access_point() = 0;
    [[nodiscard]] virtual bool connected() const noexcept = 0;
};
class ILogger {
public:
    virtual ~ILogger() = default; virtual void info(std::string_view) noexcept = 0; virtual void error(std::string_view) noexcept = 0;
};
class IClock { public: virtual ~IClock() = default; [[nodiscard]] virtual std::uint64_t milliseconds() const noexcept = 0; };
class ISchedulerClock : public IClock { public: [[nodiscard]] virtual std::uint8_t weekday() const noexcept = 0; [[nodiscard]] virtual std::uint16_t minute_of_day() const noexcept = 0; [[nodiscard]] virtual bool synchronized() const noexcept = 0; };
} // namespace atlas

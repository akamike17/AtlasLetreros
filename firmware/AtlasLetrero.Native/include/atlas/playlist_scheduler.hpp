#pragma once
#include "controller_runtime.hpp"

namespace atlas {
class PlaylistScheduler final {
public:
    PlaylistScheduler(ControllerRuntime& runtime,IConfigurationStore& storage,const ISchedulerClock& clock);
    bool boot(); bool save(const Playlist& playlist); bool save(const Schedule& schedule);
    bool play(std::string_view playlist_id); bool tick(); void stop() noexcept;
    [[nodiscard]] std::string_view active_playlist_id() const noexcept; [[nodiscard]] std::string_view active_schedule_id() const noexcept;
private:
    const Playlist* find_playlist(std::string_view id) const noexcept; const Schedule* select_schedule() const noexcept;
    bool activate(const Playlist& playlist,std::string_view schedule_id); bool advance(bool force);
    ControllerRuntime& runtime_; IConfigurationStore& storage_; const ISchedulerClock& clock_;
    std::vector<Playlist> playlists_; std::vector<Schedule> schedules_; std::string active_playlist_,active_schedule_; std::size_t item_index_{}; std::uint64_t item_started_{}; bool exhausted_{};
};
} // namespace atlas

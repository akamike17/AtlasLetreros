#include "atlas/driver_registry.hpp"

namespace atlas {
bool DriverRegistry::register_driver(std::string id, DriverFactory factory) {
    if (id.empty() || !factory) return false;
    return factories_.emplace(std::move(id), factory).second;
}
std::unique_ptr<IDisplayDriver> DriverRegistry::create(std::string_view id) const {
    const auto found = factories_.find(std::string{id}); return found == factories_.end() ? nullptr : found->second();
}
} // namespace atlas

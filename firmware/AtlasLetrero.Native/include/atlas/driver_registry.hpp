#pragma once
#include "display_driver.hpp"
#include <string>
#include <unordered_map>

namespace atlas {
class DriverRegistry final {
public:
    bool register_driver(std::string id, DriverFactory factory);
    [[nodiscard]] std::unique_ptr<IDisplayDriver> create(std::string_view id) const;
    [[nodiscard]] std::size_t size() const noexcept { return factories_.size(); }
private:
    std::unordered_map<std::string, DriverFactory> factories_;
};
} // namespace atlas

#pragma once
#include "platform.hpp"

namespace atlas {
class IEntropySource { public: virtual ~IEntropySource()=default;virtual bool fill(std::vector<std::uint8_t>&bytes)=0; };
class IDeviceIdentityStore { public: virtual ~IDeviceIdentityStore()=default;virtual std::optional<DeviceIdentity> load_identity()=0;virtual bool save_identity(const DeviceIdentity&)=0; };
class DeviceIdentityManager final {
public:DeviceIdentityManager(IDeviceIdentityStore&store,IEntropySource&entropy):store_(store),entropy_(entropy){}[[nodiscard]] std::optional<DeviceIdentity> ensure();static std::optional<std::string> create_pairing_token(IEntropySource&entropy);
private:IDeviceIdentityStore&store_;IEntropySource&entropy_;
};
enum class AuthenticationResult : std::uint8_t { accepted,missing,invalid,locked };
class LocalAuthenticator final {
public:
    LocalAuthenticator(std::string pairing_token,const IClock& clock,std::uint8_t maximum_failures=5,std::uint32_t lockout_milliseconds=30'000);
    [[nodiscard]] AuthenticationResult authenticate(std::string_view token) noexcept;
    [[nodiscard]] bool pairing_required() const noexcept { return token_.empty(); }
private:
    static bool constant_time_equal(std::string_view left,std::string_view right) noexcept;
    std::string token_;const IClock&clock_;std::uint8_t maximum_failures_,failures_{};std::uint32_t lockout_milliseconds_;std::uint64_t locked_until_{};
};
} // namespace atlas

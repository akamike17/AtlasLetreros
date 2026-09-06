#include "atlas/security.hpp"
#include <algorithm>

namespace atlas {
namespace {std::string hex(const std::vector<std::uint8_t>&bytes){static constexpr char digits[]="0123456789abcdef";std::string value;value.reserve(bytes.size()*2);for(const auto byte:bytes){value.push_back(digits[byte>>4]);value.push_back(digits[byte&15]);}return value;}}
std::optional<DeviceIdentity> DeviceIdentityManager::ensure(){if(const auto existing=store_.load_identity();existing&&!existing->id.empty()&&!existing->serial_number.empty()&&!existing->host_name.empty())return existing;std::vector<std::uint8_t>bytes(16);if(!entropy_.fill(bytes))return std::nullopt;const auto value=hex(bytes);DeviceIdentity identity{"atlas-"+value,value,"atlas-"+value.substr(0,8)};return store_.save_identity(identity)?std::optional<DeviceIdentity>{identity}:std::nullopt;}
std::optional<std::string> DeviceIdentityManager::create_pairing_token(IEntropySource&entropy){std::vector<std::uint8_t>bytes(32);return entropy.fill(bytes)?std::optional<std::string>{hex(bytes)}:std::nullopt;}
LocalAuthenticator::LocalAuthenticator(std::string token,const IClock&clock,std::uint8_t maximum_failures,std::uint32_t lockout):token_(std::move(token)),clock_(clock),maximum_failures_(std::max<std::uint8_t>(1,maximum_failures)),lockout_milliseconds_(lockout){}
bool LocalAuthenticator::constant_time_equal(std::string_view a,std::string_view b)noexcept{const auto size=std::max(a.size(),b.size());std::size_t difference=a.size()^b.size();for(std::size_t i{};i<size;++i)difference|=static_cast<unsigned char>(i<a.size()?a[i]:0)^static_cast<unsigned char>(i<b.size()?b[i]:0);return difference==0;}
AuthenticationResult LocalAuthenticator::authenticate(std::string_view token)noexcept{const auto now=clock_.milliseconds();if(now<locked_until_)return AuthenticationResult::locked;if(token.empty())return AuthenticationResult::missing;if(!token_.empty()&&constant_time_equal(token_,token)){failures_=0;locked_until_=0;return AuthenticationResult::accepted;}if(++failures_>=maximum_failures_){failures_=0;locked_until_=now+lockout_milliseconds_;return AuthenticationResult::locked;}return AuthenticationResult::invalid;}
} // namespace atlas

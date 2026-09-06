#pragma once
#include "platform.hpp"

namespace atlas {
class IFirmwareVerifier { public: virtual ~IFirmwareVerifier()=default;virtual bool verify(const std::vector<std::uint8_t>&image,const std::vector<std::uint8_t>&signature)=0; };
class IOtaPlatform { public: virtual ~IOtaPlatform()=default;virtual bool can_install(std::string_view version)const=0;virtual bool stage(const std::vector<std::uint8_t>&image,std::string_view version)=0;virtual bool activate_on_reboot()=0;virtual bool confirm_running()=0;virtual void rollback()noexcept=0; };
enum class OtaResult : std::uint8_t { accepted,invalid,too_large,signature_rejected,downgrade_rejected,stage_failed };
class OtaManager final {
public:
    static constexpr std::size_t maximum_firmware_bytes=4*1024*1024;
    OtaManager(IFirmwareVerifier&verifier,IOtaPlatform&platform):verifier_(verifier),platform_(platform){}
    OtaResult stage(std::string_view version,const std::vector<std::uint8_t>&image,const std::vector<std::uint8_t>&signature);
    bool confirm_boot(bool healthy) noexcept;
private:IFirmwareVerifier&verifier_;IOtaPlatform&platform_;
};
class IRecoveryPlatform { public: virtual ~IRecoveryPlatform()=default;virtual bool backup_configuration()=0;virtual bool restore_configuration()=0;virtual std::uint8_t boot_failures()const noexcept=0;virtual void set_boot_failures(std::uint8_t)noexcept=0;virtual void enter_safe_mode()noexcept=0; };
class RecoveryManager final {
public:explicit RecoveryManager(IRecoveryPlatform&platform,std::uint8_t threshold=3):platform_(platform),threshold_(threshold){}bool backup();bool record_boot(bool healthy);bool restore();
private:IRecoveryPlatform&platform_;std::uint8_t threshold_;
};
} // namespace atlas

#include "atlas/ota_recovery.hpp"
#include <cctype>

namespace atlas { namespace {
bool valid_version(std::string_view value){if(value.empty()||value.size()>32)return false;for(const auto c:value)if(!std::isalnum(static_cast<unsigned char>(c))&&c!='.'&&c!='-'&&c!='_')return false;return true;}
}
OtaResult OtaManager::stage(std::string_view version,const std::vector<std::uint8_t>&image,const std::vector<std::uint8_t>&signature){if(!valid_version(version)||image.empty()||signature.empty())return OtaResult::invalid;if(image.size()>maximum_firmware_bytes)return OtaResult::too_large;if(!platform_.can_install(version))return OtaResult::downgrade_rejected;if(!verifier_.verify(image,signature))return OtaResult::signature_rejected;if(!platform_.stage(image,version)||!platform_.activate_on_reboot()){platform_.rollback();return OtaResult::stage_failed;}return OtaResult::accepted;}
bool OtaManager::confirm_boot(bool healthy)noexcept{if(healthy)return platform_.confirm_running();platform_.rollback();return false;}
bool RecoveryManager::backup(){return platform_.backup_configuration();}bool RecoveryManager::restore(){const auto restored=platform_.restore_configuration();if(restored)platform_.set_boot_failures(0);return restored;}bool RecoveryManager::record_boot(bool healthy){if(healthy){platform_.set_boot_failures(0);return true;}const auto failures=static_cast<std::uint8_t>(platform_.boot_failures()+1);platform_.set_boot_failures(failures);if(failures<threshold_)return false;const auto recovered=restore();platform_.enter_safe_mode();return recovered;}
} // namespace atlas

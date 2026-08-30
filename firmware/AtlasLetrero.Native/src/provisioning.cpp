#include "atlas/provisioning.hpp"
#include <algorithm>
#include <cctype>

namespace atlas {
ProvisioningManager::ProvisioningManager(IConfigurationStore& storage,INetworkStack& network,ILogger& logger,INetworkCredentialVault* credential_vault):storage_(storage),network_(network),logger_(logger),credential_vault_(credential_vault){}
bool ProvisioningManager::boot(){const auto credentials=credential_vault_?credential_vault_->load():storage_.load_network();if(!credentials){state_=network_.start_access_point()?ProvisioningState::access_point:ProvisioningState::failed;logger_.info("provisioning access point");return state_==ProvisioningState::access_point;}state_=ProvisioningState::connecting;if(network_.configure_station(*credentials)&&network_.start_station()){state_=ProvisioningState::connected;return true;}network_.start_access_point();state_=ProvisioningState::access_point;logger_.error("station connection failed; access point fallback");return false;}
ProvisioningResult ProvisioningManager::submit(NetworkCredentials credentials){if(credentials.ssid.empty()||credentials.ssid.size()>32||credentials.password.size()>63)return{false,false,"invalid Wi-Fi credentials"};if(!(credential_vault_?credential_vault_->save(credentials):storage_.save_network(credentials)))return{false,false,"configuration persistence failed"};state_=ProvisioningState::connecting;if(!network_.configure_station(credentials)||!network_.start_station()){network_.start_access_point();state_=ProvisioningState::access_point;return{false,false,"Wi-Fi connection failed"};}state_=ProvisioningState::connected;return{true,true,{}};}
bool ProvisioningManager::factory_reset(){if(!(credential_vault_?credential_vault_->clear():storage_.clear_network()))return false;state_=network_.start_access_point()?ProvisioningState::access_point:ProvisioningState::failed;return state_==ProvisioningState::access_point;}
bool ProvisioningManager::valid_host_name(const std::string_view value)noexcept{return!value.empty()&&value.size()<=63&&value.front()!='-'&&value.back()!='-'&&std::all_of(value.begin(),value.end(),[](const char c){return std::isalnum(static_cast<unsigned char>(c))!=0||c=='-';});}
} // namespace atlas

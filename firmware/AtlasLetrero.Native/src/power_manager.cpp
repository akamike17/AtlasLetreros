#include "atlas/power_manager.hpp"
#include <algorithm>
#include <limits>

namespace atlas { namespace {
std::uint8_t combine(std::uint8_t a,std::uint8_t b) noexcept{return static_cast<std::uint8_t>((static_cast<std::uint16_t>(a)*b+127)/255);}
std::uint32_t saturate(std::uint64_t value) noexcept{return static_cast<std::uint32_t>(std::min<std::uint64_t>(value,std::numeric_limits<std::uint32_t>::max()));}
}
PowerManager::PowerManager(PowerProfile profile):profile_(profile){if(profile_.controller_reserve_milliamps>profile_.available_milliamps)profile_.controller_reserve_milliamps=profile_.available_milliamps;}
PowerDecision PowerManager::evaluate(FrameView frame,const std::uint8_t display_brightness)const noexcept{
    const auto requested=combine(frame.brightness,std::min(display_brightness,profile_.maximum_brightness));
    const auto budget=profile_.available_milliamps-profile_.controller_reserve_milliamps;
    std::uint64_t channel_sum=0;for(const auto&p:frame.pixels){channel_sum+=p.r;channel_sum+=p.g;channel_sum+=p.b;if(frame.color_model==ColorModel::rgbw)channel_sum+=p.w;}
    const auto full_current=(channel_sum*profile_.milliamps_per_channel+127)/255;
    auto applied=requested;if(full_current>0&&budget*255ULL<full_current*requested)applied=static_cast<std::uint8_t>(std::min<std::uint64_t>(255,(budget*255ULL)/full_current));
    const auto current=saturate((full_current*applied+127)/255);const auto watts=saturate((static_cast<std::uint64_t>(current)*profile_.supply_millivolts+500)/1000);
    return{requested,applied,current,budget,watts,applied<requested};
}
} // namespace atlas

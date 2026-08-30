#include "atlas/matrix_mapper.hpp"
#include <algorithm>
#include <stdexcept>
#include <utility>

namespace atlas {
MatrixMapper::MatrixMapper(DisplayConfiguration configuration) : configuration_(std::move(configuration)) {
    if (configuration_.width == 0 || configuration_.height == 0 || configuration_.tiles.empty() ||
        static_cast<std::size_t>(configuration_.width) * configuration_.height > maximum_pixels) throw std::invalid_argument("invalid topology");
    std::sort(configuration_.tiles.begin(), configuration_.tiles.end(), [](const Tile& left,const Tile& right){return left.chain_index<right.chain_index;}); std::size_t offset{};
    for (const auto& tile : configuration_.tiles) { offsets_.push_back(offset); offset += tile.pixel_count(); }
    if (offset != static_cast<std::size_t>(configuration_.width) * configuration_.height) throw std::invalid_argument("topology must cover canvas");
}
PhysicalAddress MatrixMapper::map(const std::uint16_t logical_x, const std::uint16_t logical_y) const {
    if (logical_x >= configuration_.width || logical_y >= configuration_.height) throw std::out_of_range("logical pixel");
    for (std::size_t i{}; i < configuration_.tiles.size(); ++i) {
        const auto& t=configuration_.tiles[i]; if(logical_x<t.logical_x||logical_y<t.logical_y||logical_x>=t.logical_x+t.width||logical_y>=t.logical_y+t.height)continue;
        std::size_t x=logical_x-t.logical_x,y=logical_y-t.logical_y;
        if(t.origin==Origin::top_right||t.origin==Origin::bottom_right)x=t.width-1-x;
        if(t.origin==Origin::bottom_left||t.origin==Origin::bottom_right)y=t.height-1-y;
        if(t.mirror==Mirror::horizontal||t.mirror==Mirror::both)x=t.width-1-x;
        if(t.mirror==Mirror::vertical||t.mirror==Mirror::both)y=t.height-1-y;
        switch(t.rotation){case Rotation::clockwise_90:{auto old=x;x=t.height-1-y;y=old;break;}case Rotation::clockwise_180:x=t.width-1-x;y=t.height-1-y;break;case Rotation::clockwise_270:{auto old=x;x=y;y=t.width-1-old;break;}default:break;}
        auto major=t.traversal==Traversal::row?y:x,minor=t.traversal==Traversal::row?x:y;
        const auto minor_size=t.traversal==Traversal::row?t.physical_width():t.physical_height();
        if(t.layout==Layout::serpentine&&(major&1U)!=0U)minor=minor_size-1-minor;
        const auto physical=major*minor_size+minor; return {i,physical,offsets_[i]+physical};
    } throw std::logic_error("topology gap");
}
std::vector<std::uint8_t> MatrixMapper::encode(const FrameView frame) const {
    if(frame.width!=configuration_.width||frame.height!=configuration_.height||frame.pixels.size()!=static_cast<std::size_t>(frame.width)*frame.height)throw std::invalid_argument("frame mismatch");
    const bool rgbw=configuration_.channel_order>=ChannelOrder::rgbw; const std::size_t channels=rgbw?4:3;
    if(rgbw!=(frame.color_model==ColorModel::rgbw))throw std::invalid_argument("color mismatch");
    std::vector<std::uint8_t> output(frame.pixels.size()*channels); const auto level=static_cast<unsigned>(frame.brightness)*configuration_.brightness/255U;
    const auto scale=[level](std::uint8_t value){return static_cast<std::uint8_t>((static_cast<unsigned>(value)*level+127U)/255U);};
    for(std::uint16_t y{};y<frame.height;++y)for(std::uint16_t x{};x<frame.width;++x){const auto p=frame.pixels[static_cast<std::size_t>(y)*frame.width+x];const std::uint8_t r=scale(p.r),g=scale(p.g),b=scale(p.b),w=scale(p.w);auto at=map(x,y).absolute_index*channels;
        switch(configuration_.channel_order){case ChannelOrder::rgb:output[at]=r;output[at+1]=g;output[at+2]=b;break;case ChannelOrder::rbg:output[at]=r;output[at+1]=b;output[at+2]=g;break;case ChannelOrder::grb:output[at]=g;output[at+1]=r;output[at+2]=b;break;case ChannelOrder::gbr:output[at]=g;output[at+1]=b;output[at+2]=r;break;case ChannelOrder::brg:output[at]=b;output[at+1]=r;output[at+2]=g;break;case ChannelOrder::bgr:output[at]=b;output[at+1]=g;output[at+2]=r;break;case ChannelOrder::rgbw:output[at]=r;output[at+1]=g;output[at+2]=b;output[at+3]=w;break;case ChannelOrder::grbw:output[at]=g;output[at+1]=r;output[at+2]=b;output[at+3]=w;break;case ChannelOrder::brgw:output[at]=b;output[at+1]=r;output[at+2]=g;output[at+3]=w;break;case ChannelOrder::wrgb:output[at]=w;output[at+1]=r;output[at+2]=g;output[at+3]=b;break;}}
    return output;
}
} // namespace atlas

#include "atlas/matrix_mapper.hpp"
#include "atlas/native_drivers.hpp"
#include <cassert>

using namespace atlas;
namespace {
class CaptureTransport final:public IAddressableTransport{public:std::uint32_t hz{};std::uint16_t reset{};std::vector<std::uint8_t>bytes;bool configure(std::uint32_t f,std::uint16_t r)override{hz=f;reset=r;return true;}bool transmit(const std::vector<std::uint8_t>&v)override{bytes=v;return true;}};
DisplayConfiguration profile(){return{16,16,ColorModel::rgb,ChannelOrder::grb,255,{{0,0,16,16,Origin::top_left,Traversal::row,Layout::serpentine}}};}
std::size_t reference(std::uint16_t x,std::uint16_t y){return y*16+(y%2==0?x:15-x);}
}
int main(){
    const auto config=profile();MatrixMapper mapper{config};for(std::uint16_t y=0;y<16;++y)for(std::uint16_t x=0;x<16;++x)assert(mapper.map(x,y).absolute_index==reference(x,y));
    CaptureTransport transport;AddressableDriver driver{transport,ws2812b_profile};assert(driver.initialize(config)&&transport.hz==800000);std::vector<Pixel>pixels(256);pixels[16]={10,20,30,0};assert(driver.render({16,16,ColorModel::rgb,255,pixels}));const auto offset=reference(0,1)*3;assert(transport.bytes.size()==768&&offset==93&&transport.bytes[offset]==20&&transport.bytes[offset+1]==10&&transport.bytes[offset+2]==30);return 0;
}

using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class FrameBufferTests
{
    [Fact]
    public void AddressingIsRowMajorAndBoundsAreEnforced()
    {
        var frame = new FrameBuffer(3, 2);
        frame[2, 1] = new(7, 8, 9);
        Assert.Equal(new Pixel(7, 8, 9), frame.Pixels[5]);
        Assert.Throws<ArgumentOutOfRangeException>(() => frame[3, 0] = Pixel.Off);
        Assert.False(frame.TrySetPixel(-1, 0, new(1, 1, 1)));
    }

    [Fact]
    public void CloneOwnsPixelsAndPreservesBrightness()
    {
        var source = new FrameBuffer(2, 2, brightness: 100);
        source[0, 0] = new(255, 0, 0);
        var clone = source.Clone();
        source.Clear();
        Assert.Equal(new Pixel(255, 0, 0), clone[0, 0]);
        Assert.Equal(100, clone.Brightness);
    }

    [Fact]
    public void BrightnessDoesNotDestroyLogicalPixelData()
    {
        var frame = new FrameBuffer(1, 1, brightness: 128);
        frame[0, 0] = new(255, 100, 1);
        Assert.Equal(new Pixel(128, 50, 1), frame.GetOutputPixel(0, 0));
        Assert.Equal(new Pixel(255, 100, 1), frame[0, 0]);
    }

    [Fact]
    public void CopyClipsAndSupportsTransparentOffPixels()
    {
        var source = new FrameBuffer(3, 2);
        source[0, 0] = new(255, 0, 0);
        source[2, 1] = new(0, 255, 0);
        var destination = new FrameBuffer(3, 2);
        destination.Clear(new(0, 0, 255));
        destination.CopyFrom(source, 0, 0, 3, 2, 1, 0, transparentOff: true);
        Assert.Equal(new Pixel(255, 0, 0), destination[1, 0]);
        Assert.Equal(new Pixel(0, 0, 255), destination[2, 0]);
    }

    [Fact]
    public void CopyRejectsDifferentColorModels()
    {
        var rgb = new FrameBuffer(1, 1);
        var rgbw = new FrameBuffer(1, 1, ColorModel.Rgbw);
        Assert.Throws<ArgumentException>(() => rgb.CopyFrom(rgbw, 0, 0, 1, 1, 0, 0));
        Assert.Throws<InvalidOperationException>(() => rgb[0, 0] = new(0, 0, 0, 1));
    }

    [Fact]
    public void MapperCombinesFrameAndTransportBrightnessOnce()
    {
        var frame = new FrameBuffer(1, 1, brightness: 128);
        frame[0, 0] = new(255, 255, 255);
        var mapper = new MatrixMapper(new MatrixTopology(1, 1, [new(0, 0, 1, 1)]));
        Assert.Equal(new byte[] { 64, 64, 64 }, mapper.Encode(frame, 128));
    }

    [Fact]
    public void RejectsUnsafeCanvasAllocation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameBuffer(2048, 2048));
    }
}

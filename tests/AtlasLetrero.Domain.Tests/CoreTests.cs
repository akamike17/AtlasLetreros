using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class CoreTests
{
    [Fact]
    public void Serpentine16x16MapsExactPixels()
    {
        var mapper = Mapper(16, 16, MatrixLayout.Serpentine);
        Assert.Equal(0, mapper.Map(0, 0).AbsoluteIndex);
        Assert.Equal(15, mapper.Map(15, 0).AbsoluteIndex);
        Assert.Equal(16, mapper.Map(15, 1).AbsoluteIndex);
        Assert.Equal(31, mapper.Map(0, 1).AbsoluteIndex);
        Assert.Equal(255, mapper.Map(0, 15).AbsoluteIndex);
    }

    [Fact]
    public void Progressive32x8MapsRows()
    {
        var mapper = Mapper(32, 8, MatrixLayout.Progressive);
        Assert.Equal(32, mapper.Map(0, 1).AbsoluteIndex);
        Assert.Equal(255, mapper.Map(31, 7).AbsoluteIndex);
    }

    [Fact]
    public void TwoTilesForm32x16Chain()
    {
        var topology = new MatrixTopology(32, 16,
        [
            new(0, 0, 16, 16, ChainIndex: 0),
            new(16, 0, 16, 16, ChainIndex: 1)
        ]);
        var mapper = new MatrixMapper(topology);
        Assert.Equal(255, mapper.Map(15, 15).AbsoluteIndex);
        Assert.Equal(256, mapper.Map(16, 0).AbsoluteIndex);
        Assert.Equal(511, mapper.Map(31, 15).AbsoluteIndex);
    }

    [Fact]
    public void FourTilesForm32x32Chain()
    {
        var mapper = new MatrixMapper(new MatrixTopology(32, 32,
        [new(0, 0, 16, 16, ChainIndex: 0), new(16, 0, 16, 16, ChainIndex: 1),
         new(0, 16, 16, 16, ChainIndex: 2), new(16, 16, 16, 16, ChainIndex: 3)]));
        Assert.Equal(512, mapper.Map(0, 16).AbsoluteIndex);
        Assert.Equal(1023, mapper.Map(31, 31).AbsoluteIndex);
    }

    [Theory]
    [InlineData(MatrixOrigin.BottomRight, MatrixMirror.None, 15)]
    [InlineData(MatrixOrigin.TopLeft, MatrixMirror.Horizontal, 3)]
    [InlineData(MatrixOrigin.TopLeft, MatrixMirror.Vertical, 12)]
    public void OriginAndMirrorsTransformCoordinates(MatrixOrigin origin, MatrixMirror mirror, int expected)
    {
        var mapper = new MatrixMapper(new MatrixTopology(4, 4, [new(0, 0, 4, 4, origin, Mirror: mirror)]));
        Assert.Equal(expected, mapper.Map(0, 0).AbsoluteIndex);
    }

    [Fact]
    public void Rotation90MapsSquareTile()
    {
        var mapper = new MatrixMapper(new MatrixTopology(4, 4, [new(0, 0, 4, 4, Rotation: MatrixRotation.Clockwise90)]));
        Assert.Equal(3, mapper.Map(0, 0).AbsoluteIndex);
        Assert.Equal(15, mapper.Map(3, 0).AbsoluteIndex);
    }

    [Theory]
    [InlineData(ChannelOrder.Rgb, new byte[] { 1, 2, 3 })]
    [InlineData(ChannelOrder.Grb, new byte[] { 2, 1, 3 })]
    [InlineData(ChannelOrder.Brg, new byte[] { 3, 1, 2 })]
    public void EncodesChannelOrder(ChannelOrder order, byte[] expected)
    {
        var frame = new FrameBuffer(1, 1); frame[0, 0] = new(1, 2, 3);
        var mapper = new MatrixMapper(new MatrixTopology(1, 1, [new(0, 0, 1, 1)], order));
        Assert.Equal(expected, mapper.Encode(frame));
    }

    [Fact]
    public void EncodesRgbw()
    {
        var frame = new FrameBuffer(1, 1, ColorModel.Rgbw); frame[0, 0] = new(1, 2, 3, 4);
        var mapper = new MatrixMapper(new MatrixTopology(1, 1, [new(0, 0, 1, 1)], ChannelOrder.Grbw));
        Assert.Equal(new byte[] { 2, 1, 3, 4 }, mapper.Encode(frame));
    }

    [Fact]
    public void DrawingClipsAndFillRespectsBoundary()
    {
        var frame = new FrameBuffer(8, 8);
        Drawing.Rectangle(frame, 1, 1, 6, 6, new(255, 0, 0));
        var changed = Drawing.Fill(frame, 2, 2, new(0, 255, 0));
        Drawing.Line(frame, -3, 0, 3, 0, new(0, 0, 255));
        Assert.Equal(16, changed);
        Assert.Equal(new Pixel(255, 0, 0), frame[1, 1]);
        Assert.Equal(new Pixel(0, 255, 0), frame[2, 2]);
        Assert.Equal(new Pixel(0, 0, 255), frame[0, 0]);
    }

    [Fact]
    public void CapabilityResolverReportsPhysicalLimits()
    {
        var controller = new ControllerCapabilities(256, 16, 16, [ColorModel.Rgb], 30, 1000, 2000, ["generic.addressable"], 1, true, false);
        var result = HardwareCapabilityResolver.Evaluate(controller,
            new(32, 16, ColorModel.Rgbw, 60, 2000, 3000, "hub75", 2, true));
        Assert.False(result.IsCompatible);
        Assert.Equal(7, result.Reasons.Count);
    }

    [Fact]
    public void PowerManagerLimitsFullWhiteFrame()
    {
        var frame = new FrameBuffer(16, 16); frame.Clear(new(255, 255, 255));
        var estimate = PowerManager.Estimate(frame, new(5, 10, 0.3m, 256, 100), 255);
        Assert.True(estimate.Limited);
        Assert.InRange(estimate.SafeBrightness, (byte)160, (byte)162);
    }

    [Fact]
    public void SceneCompositionHonorsLayerVisibilityAndBrightness()
    {
        var scene = new Scene(Guid.NewGuid(), "Test", 4, 4, TimeSpan.FromSeconds(1),
        [new("visible", [new PixelElement(1, 1, new(100, 50, 0))]),
         new("hidden", [new PixelElement(2, 2, new(255, 255, 255))], false)]);
        var frame = SceneEngine.Render(scene, TimeSpan.FromSeconds(2.5), 128);
        Assert.Equal(new Pixel(100, 50, 0), frame[1, 1]);
        Assert.Equal(new Pixel(50, 25, 0), frame.GetOutputPixel(1, 1));
        Assert.Equal(Pixel.Off, frame[2, 2]);
    }

    private static MatrixMapper Mapper(int width, int height, MatrixLayout layout) =>
        new(new MatrixTopology(width, height, [new(0, 0, width, height, Layout: layout)]));
}

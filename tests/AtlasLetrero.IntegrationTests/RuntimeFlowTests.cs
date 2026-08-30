using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using Xunit;

namespace AtlasLetrero.IntegrationTests;

public sealed class RuntimeFlowTests
{
    [Fact]
    public async Task UploadedSceneSurvivesControllerRestartAndMapsPhysicalPixel()
    {
        var topology = new MatrixTopology(16, 16, [new(0, 0, 16, 16, Layout: MatrixLayout.Serpentine)], ChannelOrder.Grb);
        var configuration = new DeviceConfiguration("atlas-test", topology);
        var store = new MemorySceneStore();
        var scene = new Scene(Guid.NewGuid(), "SE ARREGLAN COMPUTADORAS", 16, 16, TimeSpan.FromSeconds(3),
            [new("content", [new PixelElement(15, 1, new(10, 20, 30))])]);

        var firstDriver = new VirtualDisplayDriver();
        var firstRuntime = new ControllerRuntime(firstDriver, store);
        await firstRuntime.BootAsync(configuration);
        await firstRuntime.UploadAsync(scene);
        await firstRuntime.PlayAsync(scene.Id);
        Assert.Equal(new byte[] { 20, 10, 30 }, firstDriver.LastPhysicalFrame![48..51]);

        var restartedDriver = new VirtualDisplayDriver();
        var restartedRuntime = new ControllerRuntime(restartedDriver, store);
        await restartedRuntime.BootAsync(configuration);
        Assert.True(restartedRuntime.Status.IsPlaying);
        Assert.Equal(scene.Id, restartedRuntime.Status.ActiveSceneId);
        Assert.Equal(firstDriver.LastPhysicalFrame, restartedDriver.LastPhysicalFrame);
    }

    [Theory]
    [InlineData(ColorModel.Rgb)]
    [InlineData(ColorModel.Rgbw)]
    public void BinaryFrameRoundTripsPixelExactly(ColorModel model)
    {
        var source = new FrameBuffer(2, 2, model);
        source[1, 1] = new(10, 20, 30, model == ColorModel.Rgbw ? (byte)40 : (byte)0);
        var decoded = BinaryFrameCodec.Decode(BinaryFrameCodec.Encode(source));
        Assert.Equal(source[1, 1], decoded[1, 1]);
    }

    [Fact]
    public void ProtocolRejectsOversizedAndWrongVersionPayloads()
    {
        Assert.Throws<ProtocolException>(() => ProtocolJson.Deserialize<PlayRequest>(new byte[32], 16));
        var request = ProtocolJson.Serialize(new PlayRequest(99, Guid.NewGuid()));
        var decoded = ProtocolJson.Deserialize<PlayRequest>(request);
        Assert.Throws<ProtocolException>(() => ProtocolJson.ValidateVersion(decoded.ProtocolVersion));
    }
}

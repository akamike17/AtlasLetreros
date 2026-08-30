using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using Xunit;

namespace AtlasLetrero.Simulator.Tests;

public sealed class SimulatorTests
{
    [Fact]
    public async Task BinaryFrameShowsLogicalAndMappedPhysicalPixels()
    {
        var device = Device(); await device.BootAsync();
        var frame = new FrameBuffer(4, 2); frame[3, 1] = new(10, 20, 30);
        await device.ReceiveFrameAsync(BinaryFrameCodec.Encode(frame));
        var snapshot = device.Snapshot;
        Assert.True(snapshot[3, 1].IsOn);
        Assert.Equal(4, snapshot[3, 1].AbsoluteIndex);
        Assert.Equal(new byte[] { 20, 10, 30 }, snapshot.PhysicalChannels[12..15]);
        Assert.Equal(1, snapshot.FrameNumber);
    }

    [Fact]
    public async Task SnapshotExposesTilesGridOrientationAndBrightness()
    {
        var device = Device(); await device.BootAsync();
        var frame = new FrameBuffer(4, 2); frame[0, 0] = new(200, 100, 0);
        await device.ReceiveFrameAsync(BinaryFrameCodec.Encode(frame));
        await device.SetBrightnessAsync(new(1, 128));
        var snapshot = device.Snapshot;
        Assert.Equal(2, snapshot.Tiles.Count);
        Assert.Equal(MatrixLayout.Serpentine, snapshot.Tiles[0].Layout);
        Assert.Equal(1, snapshot[2, 0].TileIndex);
        Assert.Equal(128, snapshot.Brightness);
    }

    [Fact]
    public async Task AutonomousSceneContinuesAfterSimulatorRestart()
    {
        var device = Device(); await device.BootAsync();
        var scene = new Scene(Guid.NewGuid(), "Autónoma", 4, 2, TimeSpan.FromSeconds(2),
            [new("content", [new PixelElement(3, 1, new(255, 0, 0))])]);
        await device.UploadSceneAsync(SceneProtocolCodec.Encode(scene));
        await device.PlayAsync(new(1, scene.Id));
        var before = device.Snapshot.PhysicalChannels.ToArray();
        await device.RestartAsync();
        Assert.True(device.Status.IsPlaying);
        Assert.Equal(scene.Id, device.Status.ActiveSceneId);
        Assert.Equal(before, device.Snapshot.PhysicalChannels);
    }

    [Fact]
    public async Task StopClearsDisplayButKeepsStoredScene()
    {
        var device = Device(); await device.BootAsync();
        var scene = new Scene(Guid.NewGuid(), "Stop", 4, 2, TimeSpan.FromSeconds(1),
            [new("content", [new PixelElement(0, 0, new(255, 0, 0))])]);
        await device.UploadSceneAsync(SceneProtocolCodec.Encode(scene));
        await device.PlayAsync(new(1, scene.Id));
        await device.StopAsync(new(1));
        Assert.False(device.Status.IsPlaying);
        Assert.All(device.Snapshot.Pixels, pixel => Assert.False(pixel.IsOn));
        await device.PlayAsync(new(1, scene.Id));
        Assert.True(device.Snapshot[0, 0].IsOn);
    }

    [Fact]
    public async Task RejectsFrameWithWrongDimensionsOrColorModel()
    {
        var device = Device(); await device.BootAsync();
        Assert.Throws<ProtocolException>(() => BinaryFrameCodec.Decode(new byte[2]));
        await Assert.ThrowsAsync<ProtocolException>(async () =>
            await device.ReceiveFrameAsync(BinaryFrameCodec.Encode(new FrameBuffer(2, 2))));
    }

    [Fact]
    public async Task CapabilitiesDescribeActualVirtualDisplay()
    {
        var device = Device(); await device.BootAsync();
        var capabilities = device.GetCapabilities();
        Assert.Equal((4, 2), (capabilities.Width, capabilities.Height));
        Assert.Contains("frames", capabilities.Features);
        Assert.Contains("simulator", capabilities.Features);
    }

    private static SimulatorDevice Device()
    {
        var topology = new MatrixTopology(4, 2,
            [new(0, 0, 2, 2, Layout: MatrixLayout.Serpentine, ChainIndex: 0),
             new(2, 0, 2, 2, Origin: MatrixOrigin.BottomRight, ChainIndex: 1)], ChannelOrder.Grb);
        return new(new DeviceConfiguration("atlas-sim", topology));
    }
}

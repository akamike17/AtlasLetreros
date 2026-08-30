using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using Xunit;

namespace AtlasLetrero.IntegrationTests;

public sealed class ProductEndToEndTests
{
    [Fact]
    public async Task DesignedAnimatedSignSurvivesHostDisconnectAndControllerRestartPixelExactly()
    {
        const string message = "SE ARREGLAN COMPUTADORAS";
        var topology = new MatrixTopology(128, 8,
            [new(0, 0, 128, 8, Layout: MatrixLayout.Serpentine)], ChannelOrder.Grb);
        var configuration = new DeviceConfiguration("atlas-e2e", topology);
        var device = new SimulatorDevice(configuration);
        await device.BootAsync();
        Assert.Contains("scenes", device.GetCapabilities().Features);

        var scene = new Scene(Guid.NewGuid(), message, 128, 8, TimeSpan.FromSeconds(4),
            [new Layer("mensaje", [new TextElement(message, 0, 0, new(10, 20, 30), Font())])],
            animations: [new(AnimationKind.Marquee, TimeSpan.FromSeconds(4), repeat: true)]);
        var payload = SceneProtocolCodec.Encode(scene);
        var decoded = SceneProtocolCodec.Decode(payload);
        Assert.Equal(message, decoded.Name);
        Assert.Equal(message, Assert.IsType<TextElement>(decoded.Layers.Single().Elements.Single()).Text);
        Assert.Equal(AnimationKind.Marquee, decoded.Animations.Single().Kind);

        await device.UploadSceneAsync(payload);
        await device.PlayAsync(new(ProtocolVersions.Current, scene.Id));
        var position = TimeSpan.FromMilliseconds(500);
        var hostPreview = SceneEngine.Render(scene, position);
        await device.RenderAsync(position);
        AssertSnapshot(hostPreview, topology, device.Snapshot);

        payload = []; // El host deja de conservar o transmitir el contenido.
        await device.RestartAsync();
        Assert.True(device.Status.IsPlaying);
        Assert.Equal(scene.Id, device.Status.ActiveSceneId);
        AssertSnapshot(SceneEngine.Render(scene, TimeSpan.Zero), topology, device.Snapshot);
    }

    private static void AssertSnapshot(FrameBuffer expected, MatrixTopology topology, VirtualDisplaySnapshot actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Contains(actual.Pixels, pixel => pixel.IsOn);
        for (var y = 0; y < expected.Height; y++)
        for (var x = 0; x < expected.Width; x++)
            Assert.Equal(expected.GetOutputPixel(x, y), actual[x, y].Color);
        Assert.Equal(new MatrixMapper(topology).Encode(expected), actual.PhysicalChannels);
    }

    private static FontProfile Font() => new("atlas-5x7", 5, 7, 1, new Dictionary<char, ulong>
    {
        [' '] = 0, ['?'] = Glyph("11111","00001","00010","00100","00100","00000","00100"),
        ['A'] = Glyph("01110","10001","10001","11111","10001","10001","10001"),
        ['C'] = Glyph("01111","10000","10000","10000","10000","10000","01111"),
        ['D'] = Glyph("11110","10001","10001","10001","10001","10001","11110"),
        ['E'] = Glyph("11111","10000","10000","11110","10000","10000","11111"),
        ['G'] = Glyph("01111","10000","10000","10111","10001","10001","01111"),
        ['L'] = Glyph("10000","10000","10000","10000","10000","10000","11111"),
        ['M'] = Glyph("10001","11011","10101","10101","10001","10001","10001"),
        ['N'] = Glyph("10001","11001","10101","10011","10001","10001","10001"),
        ['O'] = Glyph("01110","10001","10001","10001","10001","10001","01110"),
        ['P'] = Glyph("11110","10001","10001","11110","10000","10000","10000"),
        ['R'] = Glyph("11110","10001","10001","11110","10100","10010","10001"),
        ['S'] = Glyph("01111","10000","10000","01110","00001","00001","11110"),
        ['T'] = Glyph("11111","00100","00100","00100","00100","00100","00100"),
        ['U'] = Glyph("10001","10001","10001","10001","10001","10001","01110")
    });

    private static ulong Glyph(params string[] rows)
    {
        ulong bits = 0;
        for (var y = 0; y < rows.Length; y++)
        for (var x = 0; x < rows[y].Length; x++)
            if (rows[y][x] == '1') bits |= 1UL << (y * rows[y].Length + x);
        return bits;
    }
}

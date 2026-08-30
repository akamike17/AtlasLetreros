using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;
using Xunit;

namespace AtlasLetrero.Protocol.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public void AutonomousSceneRoundTripsAndRendersPixelExactly()
    {
        var font = new FontProfile("3x5", 3, 5, 1, new Dictionary<char, ulong> { ['A'] = 0b111_101_111_101_101, ['?'] = 0 });
        var source = new Scene(Guid.NewGuid(), "SE ARREGLAN COMPUTADORAS", 16, 16, TimeSpan.FromSeconds(4),
        [new("contenido", [new TextElement("A", 1, 2, new(10, 20, 30), font),
            new RectangleElement(0, 0, 16, 16, new(1, 2, 3), false)])],
        animations: [new(AnimationKind.Blink, TimeSpan.FromSeconds(1), repeat: true)]);
        var decoded = SceneProtocolCodec.Decode(SceneProtocolCodec.Encode(source));
        var expected = SceneEngine.Render(source, TimeSpan.FromMilliseconds(100));
        var actual = SceneEngine.Render(decoded, TimeSpan.FromMilliseconds(100));
        Assert.Equal(expected.Pixels.ToArray(), actual.Pixels.ToArray());
        Assert.Equal(source.Name, decoded.Name);
        Assert.Equal(source.Animations, decoded.Animations);
    }

    [Fact]
    public void UnsupportedImageElementIsRejectedExplicitly()
    {
        var image = new FrameBuffer(1, 1);
        var scene = new Scene(Guid.NewGuid(), "Imagen", 1, 1, TimeSpan.FromSeconds(1),
            [new("image", [new ImageElement(0, 0, image)])]);
        var exception = Assert.Throws<ProtocolException>(() => SceneProtocolCodec.Encode(scene));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void SceneRejectsWrongVersionAndExcessiveElements()
    {
        var wrongVersion = new SceneDocument(99, Guid.NewGuid(), "x", 1, 1, 1000, ColorModel.Rgb, [], []);
        Assert.Throws<ProtocolException>(() => wrongVersion.ToDomain());
        var elements = Enumerable.Range(0, 10_001).Select(_ =>
            new SceneElementDocument(SceneElementKind.Pixel, 0, 0, 0, 0, 0, 0, 0, Pixel.Off, false)).ToArray();
        var excessive = new SceneDocument(1, Guid.NewGuid(), "x", 1, 1, 1000, ColorModel.Rgb,
            [new("layer", true, elements)], []);
        Assert.Throws<ProtocolException>(() => excessive.ToDomain());
    }

    [Fact]
    public void JsonUsesCamelCaseAndEnforcesLimit()
    {
        var payload = ProtocolJson.Serialize(new PlayRequest(1, Guid.Empty));
        Assert.Contains("\"protocolVersion\"", System.Text.Encoding.UTF8.GetString(payload));
        Assert.Throws<ProtocolException>(() => ProtocolJson.Deserialize<PlayRequest>(payload, 2));
    }

    [Fact]
    public void BinaryFrameRejectsCorruptionVersionAndLength()
    {
        var frame = new FrameBuffer(2, 2); frame[1, 1] = new(1, 2, 3);
        var valid = BinaryFrameCodec.Encode(frame);
        var badMagic = valid.ToArray(); badMagic[0] = 0;
        var badVersion = valid.ToArray(); badVersion[4] = 99;
        var truncated = valid[..^1];
        Assert.Throws<ProtocolException>(() => BinaryFrameCodec.Decode(badMagic));
        Assert.Throws<ProtocolException>(() => BinaryFrameCodec.Decode(badVersion));
        Assert.Throws<ProtocolException>(() => BinaryFrameCodec.Decode(truncated));
    }

    [Fact]
    public void CommandContractsRoundTrip()
    {
        var sceneId = Guid.NewGuid();
        var play = ProtocolJson.Deserialize<PlayRequest>(ProtocolJson.Serialize(new PlayRequest(1, sceneId)));
        var brightness = ProtocolJson.Deserialize<BrightnessRequest>(ProtocolJson.Serialize(new BrightnessRequest(1, 127)));
        Assert.Equal(sceneId, play.SceneId);
        Assert.Equal(127, brightness.Brightness);
    }
}

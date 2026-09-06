using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class SceneEngineTests
{
    [Fact]
    public void ImageAndShapesComposeInLayerOrder()
    {
        var image = new FrameBuffer(2, 2); image[0, 0] = new(255, 0, 0);
        var scene = Scene([new ImageElement(1, 1, image), new LineElement(0, 0, 3, 0, new(0, 255, 0)),
            new CircleElement(2, 2, 0, new(0, 0, 255))]);
        var frame = SceneEngine.Render(scene, TimeSpan.Zero);
        Assert.Equal(new Pixel(0, 255, 0), frame[3, 0]);
        Assert.Equal(new Pixel(255, 0, 0), frame[1, 1]);
        Assert.Equal(new Pixel(0, 0, 255), frame[2, 2]);
    }

    [Theory]
    [InlineData(AnimationKind.Blink, 750, 0, 0)]
    [InlineData(AnimationKind.Wipe, 250, 1, 0)]
    [InlineData(AnimationKind.Wipe, 750, 3, 0)]
    [InlineData(AnimationKind.Slide, 500, 2, 0)]
    [InlineData(AnimationKind.Scroll, 500, 2, 0)]
    public void SpatialAnimationsProduceObservablePixels(AnimationKind kind, int milliseconds, int expectedX, int expectedY)
    {
        var scene = Scene([new PixelElement(0, 0, new(255, 0, 0))], new(kind, TimeSpan.FromSeconds(1), repeat: true));
        var frame = SceneEngine.Render(scene, TimeSpan.FromMilliseconds(milliseconds));
        if (kind is AnimationKind.Blink or AnimationKind.Wipe) Assert.Equal(Pixel.Off, frame[expectedX, expectedY]);
        else Assert.Equal(new Pixel(255, 0, 0), frame[expectedX, expectedY]);
    }

    [Fact]
    public void FadeAndPulseUseOutputBrightnessWithoutDestroyingPixels()
    {
        var fade = Scene([new PixelElement(0, 0, new(200, 100, 0))], new(AnimationKind.Fade, TimeSpan.FromSeconds(1)));
        var frame = SceneEngine.Render(fade, TimeSpan.FromMilliseconds(500), 128);
        Assert.Equal(new Pixel(200, 100, 0), frame[0, 0]);
        Assert.InRange(frame.Brightness, (byte)63, (byte)65);
    }

    [Theory]
    [InlineData(TransitionKind.Fade)]
    [InlineData(TransitionKind.Wipe)]
    [InlineData(TransitionKind.Slide)]
    public void TransitionsHaveExactEndpoints(TransitionKind kind)
    {
        var from = new FrameBuffer(4, 1); from.Clear(new(255, 0, 0));
        var to = new FrameBuffer(4, 1); to.Clear(new(0, 0, 255));
        Assert.All(SceneEngine.Transition(from, to, kind, 0).Pixels.ToArray(), pixel => Assert.Equal(new Pixel(255, 0, 0), pixel));
        Assert.All(SceneEngine.Transition(from, to, kind, 1).Pixels.ToArray(), pixel => Assert.Equal(new Pixel(0, 0, 255), pixel));
    }

    [Fact]
    public void ZoomMaintainsCanvasAndCentersContent()
    {
        var scene = Scene([new RectangleElement(0, 0, 4, 4, new(255, 0, 0), true)],
            new(AnimationKind.Zoom, TimeSpan.FromSeconds(1)));
        var frame = SceneEngine.Render(scene, TimeSpan.FromMilliseconds(500));
        Assert.Equal(Pixel.Off, frame[0, 0]);
        Assert.Equal(new Pixel(255, 0, 0), frame[1, 1]);
        Assert.Equal(new Pixel(255, 0, 0), frame[2, 2]);
    }

    [Theory]
    [InlineData(EasingKind.Linear, 2)]
    [InlineData(EasingKind.EaseIn, 1)]
    [InlineData(EasingKind.EaseOut, 3)]
    [InlineData(EasingKind.EaseInOut, 2)]
    public void EasingProducesGoldenWipeFrame(EasingKind easing, int visiblePixels)
    {
        var scene = Scene([new RectangleElement(0, 0, 4, 1, new(255, 0, 0), true)],
            new(AnimationKind.Wipe, TimeSpan.FromSeconds(1), easing: easing));
        var frame = SceneEngine.Render(scene, TimeSpan.FromMilliseconds(500));
        Assert.Equal(visiblePixels, frame.Pixels.ToArray().Count(pixel => pixel != Pixel.Off));
    }

    private static Scene Scene(IReadOnlyList<ISceneElement> elements, Animation? animation = null) =>
        new(Guid.NewGuid(), "Escena", 4, 4, TimeSpan.FromSeconds(10), [new("contenido", elements)],
            animations: animation is null ? [] : [animation]);
}

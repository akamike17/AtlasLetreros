using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class ProductModelTests
{
    [Fact]
    public void BusinessRejectsDuplicateSerialNumber()
    {
        var business = new Business(Guid.NewGuid(), "Tacos");
        business.AddDevice(Device("ATL-001"));
        Assert.Throws<InvalidOperationException>(() => business.AddDevice(Device("atl-001")));
    }

    [Fact]
    public void DeviceAcceptsOnlyCompatibleScenes()
    {
        var device = Device("ATL-002");
        var valid = Scene(16, 16);
        device.StoreScene(valid);
        device.ActivateScene(valid.Id);
        Assert.Equal(valid.Id, device.ActiveSceneId);
        Assert.Throws<InvalidOperationException>(() => device.StoreScene(Scene(32, 16)));
    }

    [Fact]
    public void PlaylistLoopsAndHonorsRepeatDuration()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var playlist = new Playlist(Guid.NewGuid(), "Horario");
        playlist.Add(new(first, TimeSpan.FromSeconds(2), 2));
        playlist.Add(new(second, TimeSpan.FromSeconds(1)));
        Assert.Equal(first, playlist.Resolve(TimeSpan.FromSeconds(3.5)).SceneId);
        Assert.Equal(second, playlist.Resolve(TimeSpan.FromSeconds(4.5)).SceneId);
        Assert.Equal(first, playlist.Resolve(TimeSpan.FromSeconds(5.5)).SceneId);
    }

    [Fact]
    public void OvernightScheduleUsesStartDay()
    {
        var schedule = new Schedule(Guid.NewGuid(), Guid.NewGuid(), [DayOfWeek.Friday], new(22, 0), new(2, 0));
        var zone = TimeZoneInfo.Utc;
        Assert.True(schedule.IsActive(new DateTimeOffset(2026, 8, 28, 23, 0, 0, TimeSpan.Zero), zone));
        Assert.True(schedule.IsActive(new DateTimeOffset(2026, 8, 29, 1, 0, 0, TimeSpan.Zero), zone));
        Assert.False(schedule.IsActive(new DateTimeOffset(2026, 8, 29, 3, 0, 0, TimeSpan.Zero), zone));
    }

    [Fact]
    public void TextRendersAndClipsBitmapGlyph()
    {
        var font = new FontProfile("3x5", 3, 5, 1, new Dictionary<char, ulong>
        {
            ['A'] = Bits("010", "101", "111", "101", "101"), ['?'] = 0
        });
        var frame = new FrameBuffer(3, 4);
        new TextElement("A", 0, 0, new(255, 0, 0), font).Render(frame, TimeSpan.Zero);
        Assert.Equal(new Pixel(255, 0, 0), frame[1, 0]);
        Assert.Equal(Pixel.Off, frame[0, 0]);
        Assert.Equal(new Pixel(255, 0, 0), frame[2, 2]);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(5, -18)]
    [InlineData(9.999, -67)]
    [InlineData(10, 32)]
    public void MarqueeUsesGoldenHorizontalOrigin(double seconds, int expectedX)
    {
        Assert.Equal(expectedX, TextElement.MarqueeOrigin(
            32, 68, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void CertificationCannotClaimUntestedHardware()
    {
        Assert.Throws<ArgumentException>(() => new CompatibilityCertification(
            "WS2812B", "ESP32", "generic.addressable", "Autonomous", CertificationStatus.Certified,
            true, false, new(1, 0, 0)));
    }

    private static Device Device(string serial)
    {
        var topology = new MatrixTopology(16, 16, [new(0, 0, 16, 16, Layout: MatrixLayout.Serpentine)]);
        var identity = new DeviceIdentity(Guid.NewGuid(), serial, "Letrero");
        var capabilities = new ControllerCapabilities(1024, 64, 32, [ColorModel.Rgb], 60, 100_000, 1_000_000,
            ["generic.addressable"], 2, true, false);
        var manifest = new DeviceManifest(1, identity, new("esp32", "generic.addressable", capabilities),
            new("ws2812-16", topology, ColorModel.Rgb), new(5, 10, .3m, 256, 80),
            new("atlasled-test", true));
        return new(manifest);
    }

    private static Scene Scene(int width, int height) => new(Guid.NewGuid(), "Anuncio", width, height,
        TimeSpan.FromSeconds(2), [new("contenido", [new PixelElement(0, 0, new(255, 0, 0))])]);

    private static ulong Bits(params string[] rows)
    {
        ulong bits = 0;
        for (var y = 0; y < rows.Length; y++) for (var x = 0; x < rows[y].Length; x++)
            if (rows[y][x] == '1') bits |= 1UL << (y * rows[y].Length + x);
        return bits;
    }
}

using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Simulator;
using Xunit;

namespace AtlasLetrero.Application.Tests;

public sealed class DriverTests
{
    [Fact]
    public void StandardProfilesArePresetsOfGenericFamilies()
    {
        var registry = new DriverRegistry().AddStandardProfiles();
        Assert.Equal(4, registry.Profiles.Count);
        Assert.Equal(DriverKind.GenericAddressable, registry.GetProfile("WS2812B").Kind);
        Assert.Equal("generic.addressable", registry.GetProfile("sk6812.rgbw").Family);
        Assert.True(registry.GetProfile("apa102").IsKnownProfile);
        Assert.Equal(DriverKind.Hub75, registry.GetProfile("hub75").Kind);
        foreach (var profile in registry.Profiles) profile.Validate(256);
    }

    [Fact]
    public void RegistryCreatesFreshDriverAndRejectsDuplicates()
    {
        var registry = new DriverRegistry().Register("simulator.virtual", () => new VirtualDisplayDriver());
        Assert.NotSame(registry.Create("SIMULATOR.VIRTUAL"), registry.Create("simulator.virtual"));
        Assert.Throws<InvalidOperationException>(() => registry.Register("simulator.virtual", () => new VirtualDisplayDriver()));
        Assert.Throws<KeyNotFoundException>(() => registry.Create("missing"));
    }

    [Theory]
    [InlineData(-1, 800000, 80)]
    [InlineData(5, 99999, 80)]
    [InlineData(5, 800000, 0)]
    public void AddressableSettingsRejectUnsafeValues(int gpio, int frequency, int reset)
    {
        var settings = new GenericAddressableSettings(gpio, frequency, reset, ChannelOrder.Grb);
        Assert.ThrowsAny<ArgumentException>(() => settings.Validate(256));
    }

    [Fact]
    public void RgbwOrderRequiresFourChannels()
    {
        var settings = new GenericAddressableSettings(5, 800_000, 80, ChannelOrder.Grbw, 3);
        Assert.Throws<ArgumentException>(() => settings.Validate(256));
    }

    [Fact]
    public async Task RuntimeRejectsDisplayBeyondDriverCapacityBeforeInitialization()
    {
        var driver = new LimitedDriver();
        var runtime = new ControllerRuntime(driver, new MemorySceneStore());
        var topology = new MatrixTopology(16, 16, [new(0, 0, 16, 16)]);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await runtime.BootAsync(new("test", topology)));
        Assert.False(driver.Initialized);
    }

    [Fact]
    public async Task RuntimeRejectsRgbwForRgbOnlyDriver()
    {
        var driver = new LimitedDriver(maxPixels: 1024);
        var runtime = new ControllerRuntime(driver, new MemorySceneStore());
        var topology = new MatrixTopology(1, 1, [new(0, 0, 1, 1)], ChannelOrder.Rgbw);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await runtime.BootAsync(new("test", topology)));
    }

    private sealed class LimitedDriver(int maxPixels = 10) : IDisplayDriver
    {
        public bool Initialized { get; private set; }
        public string Id => "limited";
        public DriverCapabilities Capabilities { get; } = new("test", [ColorModel.Rgb], maxPixels, 1);
        public ValueTask InitializeAsync(DeviceConfiguration configuration, CancellationToken cancellationToken = default) { Initialized = true; return ValueTask.CompletedTask; }
        public ValueTask SetBrightnessAsync(byte brightness, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask RenderAsync(FrameBuffer frame, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ClearAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask TestAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}

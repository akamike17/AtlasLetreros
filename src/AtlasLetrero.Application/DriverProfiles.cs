using AtlasLetrero.Domain;

namespace AtlasLetrero.Application;

public enum DriverKind { GenericAddressable, GenericClocked, Hub75, Max7219, Custom }

public abstract record DriverSettings
{
    public abstract void Validate(int pixelCount);
    protected static void ValidatePixelCount(int pixelCount)
    {
        if (pixelCount <= 0 || pixelCount > 1_048_576) throw new ArgumentOutOfRangeException(nameof(pixelCount));
    }
}

public sealed record GenericAddressableSettings(int Gpio, int FrequencyHertz, int ResetMicroseconds,
    ChannelOrder ChannelOrder, int ChannelCount = 3, int Output = 0) : DriverSettings
{
    public override void Validate(int pixelCount)
    {
        ValidatePixelCount(pixelCount);
        if (Gpio is < 0 or > 48) throw new ArgumentOutOfRangeException(nameof(Gpio));
        if (FrequencyHertz is < 100_000 or > 2_000_000) throw new ArgumentOutOfRangeException(nameof(FrequencyHertz));
        if (ResetMicroseconds is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(ResetMicroseconds));
        if (ChannelCount is not (3 or 4)) throw new ArgumentOutOfRangeException(nameof(ChannelCount));
        if (Output < 0) throw new ArgumentOutOfRangeException(nameof(Output));
        var rgbw = ChannelOrder is ChannelOrder.Rgbw or ChannelOrder.Grbw or ChannelOrder.Brgw or ChannelOrder.Wrgb;
        if (rgbw != (ChannelCount == 4)) throw new ArgumentException("Channel order and channel count disagree.");
    }
}

public sealed record GenericClockedSettings(int DataGpio, int ClockGpio, int ClockHertz,
    ChannelOrder ChannelOrder, int Output = 0) : DriverSettings
{
    public override void Validate(int pixelCount)
    {
        ValidatePixelCount(pixelCount);
        if (DataGpio is < 0 or > 48 || ClockGpio is < 0 or > 48 || DataGpio == ClockGpio)
            throw new ArgumentException("DATA and CLOCK GPIO values must be distinct and valid.");
        if (ClockHertz is < 10_000 or > 40_000_000) throw new ArgumentOutOfRangeException(nameof(ClockHertz));
        if (Output < 0) throw new ArgumentOutOfRangeException(nameof(Output));
    }
}

public sealed record Hub75Settings(int ChainLength, int ScanRate, int ColorDepthBits, int RefreshRateHertz) : DriverSettings
{
    public override void Validate(int pixelCount)
    {
        ValidatePixelCount(pixelCount);
        if (ChainLength is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(ChainLength));
        if (ScanRate is not (4 or 8 or 16 or 32 or 64)) throw new ArgumentOutOfRangeException(nameof(ScanRate));
        if (ColorDepthBits is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(ColorDepthBits));
        if (RefreshRateHertz is < 30 or > 1000) throw new ArgumentOutOfRangeException(nameof(RefreshRateHertz));
    }
}

public sealed record DriverProfile(string Id, string DisplayName, DriverKind Kind, string Family,
    DriverSettings Settings, bool IsKnownProfile = false)
{
    public void Validate(int pixelCount)
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(Family))
            throw new InvalidOperationException("Driver profile metadata is incomplete.");
        Settings.Validate(pixelCount);
    }
}

public static class StandardDriverProfiles
{
    public static DriverProfile Ws2812B(int gpio = 5) => new("ws2812b", "WS2812B", DriverKind.GenericAddressable,
        "generic.addressable", new GenericAddressableSettings(gpio, 800_000, 80, ChannelOrder.Grb), true);

    public static DriverProfile Sk6812Rgbw(int gpio = 5) => new("sk6812.rgbw", "SK6812 RGBW", DriverKind.GenericAddressable,
        "generic.addressable", new GenericAddressableSettings(gpio, 800_000, 80, ChannelOrder.Grbw, 4), true);

    public static DriverProfile Apa102(int dataGpio = 5, int clockGpio = 18) => new("apa102", "APA102", DriverKind.GenericClocked,
        "generic.clocked", new GenericClockedSettings(dataGpio, clockGpio, 4_000_000, ChannelOrder.Bgr), true);

    public static DriverProfile Hub75() => new("hub75", "HUB75", DriverKind.Hub75,
        "hub75", new Hub75Settings(1, 16, 8, 120), true);

    public static DriverRegistry AddStandardProfiles(this DriverRegistry registry) => registry
        .RegisterProfile(Ws2812B()).RegisterProfile(Sk6812Rgbw()).RegisterProfile(Apa102()).RegisterProfile(Hub75());
}

namespace AtlasLetrero.Domain;

public sealed record DeviceIdentity
{
    public DeviceIdentity(Guid id, string serialNumber, string displayName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Device identity is required.", nameof(id));
        Id = id; SerialNumber = Required(serialNumber); DisplayName = Required(displayName);
    }
    public Guid Id { get; }
    public string SerialNumber { get; }
    public string DisplayName { get; }
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.") : value.Trim();
}

public sealed class Business
{
    private readonly List<Device> _devices = [];
    public Business(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Business identity is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Business name is required.", nameof(name));
        Id = id; Name = name.Trim();
    }
    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyList<Device> Devices => _devices;
    public void AddDevice(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (_devices.Any(current => current.Identity.Id == device.Identity.Id || string.Equals(current.Identity.SerialNumber, device.Identity.SerialNumber, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The device is already registered to this business.");
        _devices.Add(device);
    }
}

public sealed record ControllerProfile
{
    public ControllerProfile(string id, string driverFamily, ControllerCapabilities capabilities)
    { Id = Required(id); DriverFamily = Required(driverFamily); Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities)); }
    public string Id { get; }
    public string DriverFamily { get; }
    public ControllerCapabilities Capabilities { get; }
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.") : value.Trim();
}

public sealed record DisplayProfile(string Id, MatrixTopology Topology, ColorModel ColorModel, string PhysicalProfile = "PIXEL GRID")
{
    public int PixelCount => checked(Topology.Width * Topology.Height);
}

public sealed record NetworkConfiguration
{
    public NetworkConfiguration(string hostName, bool accessPointEnabled, string? wifiSsid = null)
    {
        hostName = hostName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (hostName.Length is < 1 or > 63 || hostName.Any(character => !(char.IsLetterOrDigit(character) || character == '-')) || hostName.StartsWith('-') || hostName.EndsWith('-'))
            throw new ArgumentException("Host name is invalid.", nameof(hostName));
        HostName = hostName; AccessPointEnabled = accessPointEnabled; WifiSsid = string.IsNullOrWhiteSpace(wifiSsid) ? null : wifiSsid.Trim();
    }
    public string HostName { get; }
    public bool AccessPointEnabled { get; }
    public string? WifiSsid { get; }
}

public sealed record DeviceManifest
{
    public DeviceManifest(int schemaVersion, DeviceIdentity identity, ControllerProfile controller, DisplayProfile display, PowerProfile power, NetworkConfiguration network)
    {
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        ArgumentNullException.ThrowIfNull(identity); ArgumentNullException.ThrowIfNull(controller); ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(power); ArgumentNullException.ThrowIfNull(network);
        if (power.PixelCount < display.PixelCount) throw new ArgumentException("Power profile does not cover the display pixel count.", nameof(power));
        SchemaVersion = schemaVersion; Identity = identity; Controller = controller; Display = display; Power = power; Network = network;
    }
    public int SchemaVersion { get; }
    public DeviceIdentity Identity { get; }
    public ControllerProfile Controller { get; }
    public DisplayProfile Display { get; }
    public PowerProfile Power { get; }
    public NetworkConfiguration Network { get; }
}

public sealed class Device
{
    private readonly Dictionary<Guid, Scene> _scenes = [];
    public Device(DeviceManifest manifest) => Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    public DeviceManifest Manifest { get; private set; }
    public DeviceIdentity Identity => Manifest.Identity;
    public IReadOnlyCollection<Scene> Scenes => _scenes.Values;
    public Guid? ActiveSceneId { get; private set; }
    public void UpdateManifest(DeviceManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (manifest.Identity.Id != Identity.Id) throw new InvalidOperationException("A manifest cannot replace device identity.");
        Manifest = manifest;
    }
    public void StoreScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Width != Manifest.Display.Topology.Width || scene.Height != Manifest.Display.Topology.Height || scene.ColorModel != Manifest.Display.ColorModel)
            throw new InvalidOperationException("Scene is incompatible with the configured display.");
        _scenes[scene.Id] = scene;
    }
    public void ActivateScene(Guid sceneId)
    {
        if (!_scenes.ContainsKey(sceneId)) throw new KeyNotFoundException("Scene is not stored on the device.");
        ActiveSceneId = sceneId;
    }
}

public sealed record FirmwareVersion : IComparable<FirmwareVersion>
{
    public FirmwareVersion(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0) throw new ArgumentOutOfRangeException(nameof(major));
        Major = major; Minor = minor; Patch = patch;
    }
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public int CompareTo(FirmwareVersion? other) => other is null ? 1 : Major != other.Major ? Major.CompareTo(other.Major) : Minor != other.Minor ? Minor.CompareTo(other.Minor) : Patch.CompareTo(other.Patch);
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

public enum CertificationStatus { Planned, Implemented, Simulated, HardwareTested, Certified, Deprecated }

public sealed record CompatibilityCertification
{
    public CompatibilityCertification(string panel, string controller, string driver, string mode, CertificationStatus status, bool simulatorTested, bool hardwareTested, FirmwareVersion? firmware, string? notes = null)
    {
        if (status == CertificationStatus.Certified && !hardwareTested) throw new ArgumentException("Certification requires a physical hardware test.", nameof(status));
        if (status >= CertificationStatus.Simulated && !simulatorTested && !hardwareTested) throw new ArgumentException("Status requires simulator or hardware evidence.", nameof(status));
        Panel = Required(panel); Controller = Required(controller); Driver = Required(driver); Mode = Required(mode);
        Status = status; SimulatorTested = simulatorTested; HardwareTested = hardwareTested; Firmware = firmware; Notes = notes?.Trim();
    }
    public string Panel { get; }
    public string Controller { get; }
    public string Driver { get; }
    public string Mode { get; }
    public CertificationStatus Status { get; }
    public bool SimulatorTested { get; }
    public bool HardwareTested { get; }
    public FirmwareVersion? Firmware { get; }
    public string? Notes { get; }
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.") : value.Trim();
}

namespace AtlasLetrero.App.Models;
public record DeviceStatus(bool Connected, string? Port, DeviceCapabilities? Capabilities);

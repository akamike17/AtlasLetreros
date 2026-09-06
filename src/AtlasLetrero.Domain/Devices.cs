namespace AtlasLetrero.Domain;

public sealed record ControllerCapabilities(
    int MaxPixels, int MaxWidth, int MaxHeight, ColorModel[] ColorModels,
    int MaxFramesPerSecond, long WorkingMemoryBytes, long SceneStorageBytes,
    string[] DriverFamilies, int OutputCount, bool HasNetwork, bool HasPsram,
    SceneComplexity MaxSceneComplexity = SceneComplexity.Complex);

public sealed record ProjectRequirements(int Width, int Height, ColorModel ColorModel,
    int FramesPerSecond, long RequiredWorkingMemoryBytes, long RequiredStorageBytes,
    string DriverFamily, int OutputCount, bool RequiresNetwork,
    bool RequiresPsram = false, SceneComplexity SceneComplexity = SceneComplexity.Standard);

public enum SceneComplexity { Static, Standard, Complex, PreRenderedHeavy }
public enum CapabilityIssueCode { Resolution, ColorModel, FrameRate, WorkingMemory, Storage, Driver, Outputs, Network, Psram, SceneComplexity }
public sealed record CapabilityIssue(CapabilityIssueCode Code, string Message);
public sealed record CompatibilityResult(bool IsCompatible, IReadOnlyList<CapabilityIssue> Issues)
{
    public IReadOnlyList<string> Reasons => Issues.Select(issue => issue.Message).ToArray();
}

public static class HardwareCapabilityResolver
{
    public static CompatibilityResult Evaluate(ControllerCapabilities controller, ProjectRequirements project)
    {
        ArgumentNullException.ThrowIfNull(controller); ArgumentNullException.ThrowIfNull(project);
        if (project.Width <= 0 || project.Height <= 0 || project.FramesPerSecond <= 0 ||
            project.RequiredWorkingMemoryBytes < 0 || project.RequiredStorageBytes < 0 || project.OutputCount <= 0)
            throw new ArgumentException("Project requirements contain invalid physical values.", nameof(project));
        var issues = new List<CapabilityIssue>();
        var pixels = checked(project.Width * project.Height);
        if (pixels > controller.MaxPixels || project.Width > controller.MaxWidth || project.Height > controller.MaxHeight) Add(CapabilityIssueCode.Resolution, "Resolution exceeds controller capacity.");
        if (!controller.ColorModels.Contains(project.ColorModel)) Add(CapabilityIssueCode.ColorModel, "Color model is unsupported.");
        if (project.FramesPerSecond > controller.MaxFramesPerSecond) Add(CapabilityIssueCode.FrameRate, "Required frame rate is unsupported.");
        if (project.RequiredWorkingMemoryBytes > controller.WorkingMemoryBytes) Add(CapabilityIssueCode.WorkingMemory, "Insufficient working memory.");
        if (project.RequiredStorageBytes > controller.SceneStorageBytes) Add(CapabilityIssueCode.Storage, "Insufficient scene storage.");
        if (!controller.DriverFamilies.Contains(project.DriverFamily, StringComparer.OrdinalIgnoreCase)) Add(CapabilityIssueCode.Driver, "Display driver is unsupported.");
        if (project.OutputCount > controller.OutputCount) Add(CapabilityIssueCode.Outputs, "Insufficient physical outputs.");
        if (project.RequiresNetwork && !controller.HasNetwork) Add(CapabilityIssueCode.Network, "Network capability is required.");
        if (project.RequiresPsram && !controller.HasPsram) Add(CapabilityIssueCode.Psram, "PSRAM is required.");
        if (project.SceneComplexity > controller.MaxSceneComplexity) Add(CapabilityIssueCode.SceneComplexity, "Scene complexity exceeds controller capacity.");
        return new(issues.Count == 0, issues);

        void Add(CapabilityIssueCode code, string message) => issues.Add(new(code, message));
    }
}

public sealed record ControllerCandidate
{
    public ControllerCandidate(string id, ControllerCapabilities capabilities, decimal unitCost)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Controller id is required.", nameof(id));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));
        Id = id.Trim(); Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities)); UnitCost = unitCost;
    }
    public string Id { get; }
    public ControllerCapabilities Capabilities { get; }
    public decimal UnitCost { get; }
}

public sealed record ControllerSelection(ControllerCandidate Candidate, CompatibilityResult Compatibility);

public static class ControllerSelector
{
    public static ControllerSelection SelectCheapest(IEnumerable<ControllerCandidate> candidates, ProjectRequirements project)
    {
        ArgumentNullException.ThrowIfNull(candidates); ArgumentNullException.ThrowIfNull(project);
        var evaluated = candidates.Select(candidate =>
        {
            if (string.IsNullOrWhiteSpace(candidate.Id) || candidate.UnitCost < 0) throw new ArgumentException("Controller candidate is invalid.", nameof(candidates));
            return new ControllerSelection(candidate, HardwareCapabilityResolver.Evaluate(candidate.Capabilities, project));
        }).ToArray();
        return evaluated.Where(selection => selection.Compatibility.IsCompatible)
            .OrderBy(selection => selection.Candidate.UnitCost)
            .ThenBy(selection => selection.Candidate.Capabilities.WorkingMemoryBytes)
            .FirstOrDefault() ?? throw new InvalidOperationException(BuildFailure(evaluated));
    }

    private static string BuildFailure(IEnumerable<ControllerSelection> evaluated)
    {
        var details = evaluated.Select(selection => $"{selection.Candidate.Id}: {string.Join(", ", selection.Compatibility.Reasons)}");
        return "No compatible controller was found. " + string.Join(" | ", details);
    }
}

public sealed record PowerProfile(decimal SupplyVoltage, decimal AvailableCurrentAmps,
    decimal ControllerReserveAmps, int PixelCount, decimal MaximumBrightnessPercent);

public sealed record PowerEstimate(decimal RequestedCurrentAmps, decimal AllowedCurrentAmps, byte SafeBrightness, bool Limited);

public static class PowerManager
{
    public static PowerEstimate Estimate(FrameBuffer frame, PowerProfile profile, byte requestedBrightness)
    {
        if (profile.SupplyVoltage <= 0 || profile.AvailableCurrentAmps <= 0 || profile.PixelCount < frame.Pixels.Length)
            throw new ArgumentException("Invalid or undersized power profile.", nameof(profile));
        var channelTotal = 0m;
        foreach (var pixel in frame.Pixels) channelTotal += (pixel.R + pixel.G + pixel.B + pixel.W) / 255m;
        var modelChannels = frame.ColorModel == ColorModel.Rgbw ? 4m : 3m;
        var fullCurrent = channelTotal * (0.06m / modelChannels);
        var requested = fullCurrent * requestedBrightness / 255m + profile.ControllerReserveAmps;
        var allowedBrightness = (byte)Math.Clamp(decimal.Floor(255m * profile.MaximumBrightnessPercent / 100m), 0, 255);
        var availableForPixels = Math.Max(0, profile.AvailableCurrentAmps - profile.ControllerReserveAmps);
        if (fullCurrent > 0) allowedBrightness = Math.Min(allowedBrightness, (byte)Math.Clamp(decimal.Floor(255m * availableForPixels / fullCurrent), 0, 255));
        var safe = Math.Min(requestedBrightness, allowedBrightness);
        return new(requested, profile.AvailableCurrentAmps, safe, safe < requestedBrightness);
    }
}

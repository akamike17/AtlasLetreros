using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class CapabilityResolverTests
{
    [Fact]
    public void ReportsPsramAndComplexityAsStructuredIssues()
    {
        var controller = Capabilities(256, false, SceneComplexity.Standard);
        var project = Requirements(16, 16, requiresPsram: true, complexity: SceneComplexity.PreRenderedHeavy);
        var result = HardwareCapabilityResolver.Evaluate(controller, project);
        Assert.False(result.IsCompatible);
        Assert.Contains(result.Issues, issue => issue.Code == CapabilityIssueCode.Psram);
        Assert.Contains(result.Issues, issue => issue.Code == CapabilityIssueCode.SceneComplexity);
    }

    [Fact]
    public void SelectsCheapestCompatibleControllerNotMostPowerful()
    {
        var basic = new ControllerCandidate("ESP económico", Capabilities(256, false, SceneComplexity.Standard), 120m);
        var s3 = new ControllerCandidate("ESP32-S3 PSRAM", Capabilities(4096, true, SceneComplexity.PreRenderedHeavy), 310m);
        var selection = ControllerSelector.SelectCheapest([s3, basic], Requirements(16, 16));
        Assert.Equal("ESP económico", selection.Candidate.Id);
        Assert.True(selection.Compatibility.IsCompatible);
    }

    [Fact]
    public void SelectsPsramControllerWhenProjectPhysicallyRequiresIt()
    {
        var basic = new ControllerCandidate("ESP económico", Capabilities(1024, false, SceneComplexity.Standard), 100m);
        var s3 = new ControllerCandidate("ESP32-S3 PSRAM", Capabilities(4096, true, SceneComplexity.Complex), 250m);
        var selection = ControllerSelector.SelectCheapest([basic, s3], Requirements(32, 16, true, SceneComplexity.Complex));
        Assert.Equal("ESP32-S3 PSRAM", selection.Candidate.Id);
    }

    [Fact]
    public void EqualCostUsesSmallestSufficientMemoryAsTieBreaker()
    {
        var small = new ControllerCandidate("small", Capabilities(512, false, SceneComplexity.Standard, 100_000), 100m);
        var large = new ControllerCandidate("large", Capabilities(2048, true, SceneComplexity.Complex, 500_000), 100m);
        Assert.Equal("small", ControllerSelector.SelectCheapest([large, small], Requirements(16, 16)).Candidate.Id);
    }

    [Fact]
    public void FailureExplainsEveryCandidate()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ControllerSelector.SelectCheapest(
            [new("tiny", Capabilities(64, false, SceneComplexity.Static), 50m)], Requirements(32, 16)));
        Assert.Contains("tiny", exception.Message);
        Assert.Contains("Resolution", exception.Message);
    }

    [Fact]
    public void InvalidRequirementsAreRejectedBeforeEvaluation()
    {
        var invalid = Requirements(0, 16);
        Assert.Throws<ArgumentException>(() => HardwareCapabilityResolver.Evaluate(Capabilities(256, false, SceneComplexity.Standard), invalid));
    }

    private static ControllerCapabilities Capabilities(int pixels, bool psram, SceneComplexity complexity, long memory = 200_000) =>
        new(pixels, pixels, pixels, [ColorModel.Rgb], 60, memory, 2_000_000,
            ["generic.addressable"], 2, true, psram, complexity);

    private static ProjectRequirements Requirements(int width, int height, bool requiresPsram = false,
        SceneComplexity complexity = SceneComplexity.Standard) =>
        new(width, height, ColorModel.Rgb, 30, 50_000, 100_000, "generic.addressable", 1, true,
            requiresPsram, complexity);
}

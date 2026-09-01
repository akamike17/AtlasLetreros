using System.Net;
using AtlasLetrero.Domain;
using AtlasLetrero.Application;
using AtlasLetrero.Infrastructure;
using AtlasLetrero.Protocol;
using AtlasLetrero.Simulator;
using AtlasLetreros.Controllers;
using AtlasLetreros.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AtlasLetrero.IntegrationTests;

public sealed class DeviceGatewayTests
{
    private static readonly DiscoveredDevice Physical = new("atlas-physical", "atlasled-test",
        IPAddress.Parse("10.20.30.40"), 8080, 1, "2.0");

    [Fact]
    public async Task CatalogRemovesDeviceThatDisappearsOnSuccessfulRefresh()
    {
        var discovery = new SequenceDiscovery([[Physical], []]);
        var catalog = new DeviceCatalog(discovery, new StubClientFactory(new RecordingHandler(_ => NoContent())));

        await catalog.RefreshAsync(default);
        Assert.True(catalog.TryResolve(Physical.DeviceId, out _));
        await catalog.RefreshAsync(default);

        Assert.False(catalog.TryResolve(Physical.DeviceId, out _));
    }

    [Fact]
    public async Task GatewayRoutesKnownDeviceOnlyAndRejectsUnknownOrArbitraryHost()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath == "/api/capabilities"
            ? Json(new CapabilitiesResponse(1, Physical.DeviceId, 32, 16, ColorModel.Rgb, ["scenes"], ["led"], 0))
            : NoContent());
        var (controller, catalog) = CreateController(handler);
        await catalog.RefreshAsync(default);

        var known = Assert.IsType<OkObjectResult>(await controller.Capabilities(Physical.DeviceId, default));
        var unknown = Assert.IsType<ObjectResult>(await controller.Capabilities("http://evil.example", default));

        Assert.NotNull(known.Value);
        Assert.Equal(404, unknown.StatusCode);
        Assert.Single(handler.Uris);
        Assert.Equal("http://10.20.30.40:8080/api/capabilities", handler.Uris[0].ToString());
    }

    [Fact]
    public async Task SceneCanvasMismatchIsControlledAndDoesNotUpload()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath == "/api/capabilities"
            ? Json(new CapabilitiesResponse(1, Physical.DeviceId, 64, 16, ColorModel.Rgb, ["scenes"], ["led"], 0))
            : NoContent());
        var (controller, catalog) = CreateController(handler);
        await catalog.RefreshAsync(default);
        var scene = new Scene(Guid.NewGuid(), "32x16", 32, 16, TimeSpan.FromSeconds(1), []);

        var result = Assert.IsType<ObjectResult>(await controller.Scene(Physical.DeviceId,
            new SemanticDesignRequest(1, 255, SceneDocument.FromDomain(scene)), default));

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("CanvasMismatch", Assert.IsType<ProblemDetails>(result.Value).Title);
        Assert.DoesNotContain(handler.Uris, uri => uri.AbsolutePath == "/api/scene");
    }

    [Fact]
    public async Task IncompatibleCommandProtocolIsRejectedBeforePhysicalRequest()
    {
        var handler = new RecordingHandler(_ => NoContent());
        var (controller, catalog) = CreateController(handler);
        await catalog.RefreshAsync(default);

        var result = Assert.IsType<ObjectResult>(await controller.Stop(Physical.DeviceId, new StopRequest(99), default));

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("ProtocolMismatch", Assert.IsType<ProblemDetails>(result.Value).Title);
        Assert.Empty(handler.Uris);
    }

    [Fact]
    public async Task PhysicalTimeoutReturnsControlledUnavailableWithoutAffectingCatalog()
    {
        var handler = new RecordingHandler(_ => throw new TaskCanceledException("timeout"));
        var (controller, catalog) = CreateController(handler);
        await catalog.RefreshAsync(default);

        var result = Assert.IsType<ObjectResult>(await controller.Status(Physical.DeviceId, default));

        Assert.Equal(503, result.StatusCode);
        Assert.Equal("DeviceUnavailable", Assert.IsType<ProblemDetails>(result.Value).Title);
        Assert.True(catalog.TryResolve(Physical.DeviceId, out _));
    }

    [Fact]
    public async Task IncompatiblePhysicalProtocolIsIsolatedInDeviceListing()
    {
        var handler = new RecordingHandler(_ => Json(new CapabilitiesResponse(99, Physical.DeviceId,
            32, 16, ColorModel.Rgb, ["scenes"], ["led"], 0)));
        var (controller, _) = CreateController(handler);

        var result = Assert.IsType<OkObjectResult>(await controller.Devices(default));
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);

        Assert.Contains("atlas-simulator", json);
        Assert.Contains("atlas-physical", json);
        Assert.Contains("\"online\":false", json, StringComparison.OrdinalIgnoreCase);
    }

    private static (DeviceController Controller, DeviceCatalog Catalog) CreateController(RecordingHandler handler)
    {
        var catalog = new DeviceCatalog(new SequenceDiscovery([[Physical]]), new StubClientFactory(handler));
        var topology = new MatrixTopology(32, 16, [new MatrixTile(0, 0, 32, 16)], ChannelOrder.Grb);
        return (new DeviceController(new SimulatorDevice(new DeviceConfiguration("atlas-simulator", topology)), catalog), catalog);
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK)
        { Content = new ByteArrayContent(ProtocolJson.Serialize(value)) };
    private static HttpResponseMessage NoContent() => new(HttpStatusCode.NoContent) { Content = new ByteArrayContent([]) };

    private sealed class SequenceDiscovery(IEnumerable<IReadOnlyList<DiscoveredDevice>> values) : IDeviceDiscovery
    {
        private readonly Queue<IReadOnlyList<DiscoveredDevice>> _values = new(values);
        public ValueTask<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_values.Count > 1 ? _values.Dequeue() : _values.Peek());
    }

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<Uri> Uris { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uris.Add(request.RequestUri!);
            return Task.FromResult(response(request));
        }
    }
}

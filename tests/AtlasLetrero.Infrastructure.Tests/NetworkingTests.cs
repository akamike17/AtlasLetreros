using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using AtlasLetrero.Domain;
using AtlasLetrero.Infrastructure;
using AtlasLetrero.Protocol;
using Xunit;

namespace AtlasLetrero.Infrastructure.Tests;

public sealed class NetworkingTests
{
    [Fact]
    public void DiscoveryAnnouncementRoundTripsAndProvidesMdnsName()
    {
        var source = new DiscoveredDevice("atlas-A82F", "atlasled-a82f", IPAddress.Parse("192.168.1.20"), 80, 1, "1.0.0");
        var decoded = DiscoveryProtocol.DecodeAnnouncement(DiscoveryProtocol.EncodeAnnouncement(source));
        Assert.Equal(source, decoded);
        Assert.Equal("atlasled-a82f.local", decoded.MdnsName);
        Assert.Equal("_atlasled._tcp.local", DiscoveryProtocol.MdnsService);
    }

    [Theory]
    [InlineData("bad host")]
    [InlineData("-bad")]
    [InlineData("bad-")]
    public void DiscoveryRejectsInvalidHostNames(string host)
    {
        var device = new DiscoveredDevice("id", host, IPAddress.Loopback, 80, 1, "1.0");
        Assert.Throws<ProtocolException>(() => DiscoveryProtocol.EncodeAnnouncement(device));
    }

    [Fact]
    public async Task HttpClientUsesVersionedEndpointsAndBinaryContentType()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/status" => Json(new StatusResponse(1, "atlas", false, null, 255, 10, "1.0", null)),
            _ => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) }
        });
        var client = new AtlasControllerClient(new HttpClient(handler) { BaseAddress = new("http://127.0.0.1:8080/") });
        Assert.Equal("atlas", (await client.GetStatusAsync()).DeviceId);
        var frame = new FrameBuffer(1, 1); await client.SendFrameAsync(frame);
        Assert.Equal(["/api/status", "/api/frame"], handler.Paths);
        Assert.Equal("application/vnd.atlas.frame", handler.LastContentType);
    }

    [Fact]
    public async Task CommandsUseExpectedPathsAndProtocolVersion()
    {
        var handler = new RecordingHandler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) });
        var client = new AtlasControllerClient(new HttpClient(handler) { BaseAddress = new("http://device/") });
        await client.PlayAsync(Guid.NewGuid()); await client.StopAsync(); await client.SetBrightnessAsync(100);
        Assert.Equal(["/api/play", "/api/stop", "/api/brightness"], handler.Paths);
        Assert.All(handler.Bodies, body => Assert.Contains("\"protocolVersion\":1", body));
    }

    [Fact]
    public async Task ControllerErrorIncludesActionableBoundedDetail()
    {
        var handler = new RecordingHandler(_ => new(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid topology") });
        var client = new AtlasControllerClient(new HttpClient(handler) { BaseAddress = new("http://device/") });
        var exception = await Assert.ThrowsAsync<ControllerCommunicationException>(async () => await client.StopAsync());
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("Invalid topology", exception.Message);
    }

    [Fact]
    public async Task RejectsOversizedControllerResponse()
    {
        var content = new ByteArrayContent(new byte[1_048_577]);
        var handler = new RecordingHandler(_ => new(HttpStatusCode.OK) { Content = content });
        var client = new AtlasControllerClient(new HttpClient(handler) { BaseAddress = new("http://device/") });
        await Assert.ThrowsAsync<ControllerCommunicationException>(async () => await client.GetStatusAsync());
    }

    [Fact]
    public async Task UdpDiscoveryDeduplicatesAndIsolatesMalformedAnnouncements()
    {
        using var responder = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)responder.Client.LocalEndPoint!).Port;
        var responseTask = Task.Run(async () =>
        {
            var query = await responder.ReceiveAsync();
            Assert.Equal(DiscoveryProtocol.Query, Encoding.ASCII.GetString(query.Buffer));
            await responder.SendAsync(Encoding.UTF8.GetBytes("malformed"), query.RemoteEndPoint);
            var first = new DiscoveredDevice("atlas-1", "first", IPAddress.Loopback, 80, 1, "1.0");
            var updated = first with { HostName = "updated" };
            await responder.SendAsync(DiscoveryProtocol.EncodeAnnouncement(first), query.RemoteEndPoint);
            await responder.SendAsync(DiscoveryProtocol.EncodeAnnouncement(updated), query.RemoteEndPoint);
        });

        var devices = await new UdpLanDiscovery(port, IPAddress.Loopback)
            .DiscoverAsync(TimeSpan.FromMilliseconds(200));
        await responseTask;

        var device = Assert.Single(devices);
        Assert.Equal("atlas-1", device.DeviceId);
        Assert.Equal("updated", device.HostName);
    }

    [Fact]
    public async Task UdpDiscoveryHonorsBoundedTimeoutWithoutResponses()
    {
        using var unused = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)unused.Client.LocalEndPoint!).Port;
        var stopwatch = Stopwatch.StartNew();

        var devices = await new UdpLanDiscovery(port, IPAddress.Loopback)
            .DiscoverAsync(TimeSpan.FromMilliseconds(100));

        Assert.Empty(devices);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(75), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ClientCoversCapabilitiesScenePlayStopAndBrightnessOnSameBaseAddress()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath == "/api/capabilities"
            ? Json(new CapabilitiesResponse(1, "atlas", 1, 1, ColorModel.Rgb, ["scenes"], ["virtual"], 0))
            : new(HttpStatusCode.NoContent) { Content = new ByteArrayContent([]) });
        var client = new AtlasControllerClient(new HttpClient(handler) { BaseAddress = new("http://10.0.0.8:8080/") });
        var scene = new Scene(Guid.NewGuid(), "scene", 1, 1, TimeSpan.FromSeconds(1),
            [new("layer", [new PixelElement(0, 0, new Pixel(1, 2, 3))])]);

        await client.GetCapabilitiesAsync();
        await client.UploadSceneAsync(scene);
        await client.PlayAsync(scene.Id);
        await client.StopAsync();
        await client.SetBrightnessAsync(17);

        Assert.Equal(["/api/capabilities", "/api/scene", "/api/play", "/api/stop", "/api/brightness"], handler.Paths);
        Assert.All(handler.Hosts, host => Assert.Equal("10.0.0.8:8080", host));
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK)
    { Content = new ByteArrayContent(ProtocolJson.Serialize(value)) };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public List<string> Bodies { get; } = [];
        public List<string> Hosts { get; } = [];
        public string? LastContentType { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            Hosts.Add(request.RequestUri.Authority);
            if (request.Content is not null)
            {
                LastContentType = request.Content.Headers.ContentType?.MediaType;
                Bodies.Add(Encoding.UTF8.GetString(await request.Content.ReadAsByteArrayAsync(cancellationToken)));
            }
            return response(request);
        }
    }
}

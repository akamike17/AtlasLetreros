using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using AtlasLetrero.Domain;
using AtlasLetrero.Protocol;

namespace AtlasLetrero.Infrastructure;

public sealed class AtlasControllerClient
{
    private readonly HttpClient _http;
    public AtlasControllerClient(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (_http.BaseAddress is null || !_http.BaseAddress.IsAbsoluteUri) throw new ArgumentException("Controller base address is required.", nameof(httpClient));
    }

    public ValueTask<StatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<StatusResponse>("api/status", cancellationToken);
    public ValueTask<CapabilitiesResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<CapabilitiesResponse>("api/capabilities", cancellationToken);

    public async ValueTask SendFrameAsync(FrameBuffer frame, CancellationToken cancellationToken = default) =>
        await SendAsync(HttpMethod.Post, "api/frame", BinaryFrameCodec.Encode(frame), "application/vnd.atlas.frame", cancellationToken);

    public async ValueTask UploadSceneAsync(Scene scene, CancellationToken cancellationToken = default) =>
        await SendAsync(HttpMethod.Post, "api/scene", SceneProtocolCodec.Encode(scene), "application/json", cancellationToken);

    public async ValueTask PlayAsync(Guid sceneId, CancellationToken cancellationToken = default) =>
        await SendJsonAsync("api/play", new PlayRequest(ProtocolVersions.Current, sceneId), cancellationToken);
    public async ValueTask StopAsync(CancellationToken cancellationToken = default) =>
        await SendJsonAsync("api/stop", new StopRequest(ProtocolVersions.Current), cancellationToken);
    public async ValueTask SetBrightnessAsync(byte brightness, CancellationToken cancellationToken = default) =>
        await SendJsonAsync("api/brightness", new BrightnessRequest(ProtocolVersions.Current, brightness), cancellationToken);

    private async ValueTask<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var payload = await ReadBoundedAsync(response, 1_048_576, cancellationToken);
        EnsureSuccess(response, payload);
        var result = ProtocolJson.Deserialize<T>(payload);
        switch (result)
        {
            case StatusResponse status: ProtocolJson.ValidateVersion(status.ProtocolVersion); break;
            case CapabilitiesResponse capabilities: ProtocolJson.ValidateVersion(capabilities.ProtocolVersion); break;
        }
        return result;
    }

    private async ValueTask SendJsonAsync<T>(string path, T value, CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Post, path, ProtocolJson.Serialize(value), "application/json", cancellationToken);

    private async ValueTask SendAsync(HttpMethod method, string path, byte[] payload, string mediaType, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path) { Content = new ByteArrayContent(payload) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var responsePayload = await ReadBoundedAsync(response, 64 * 1024, cancellationToken);
        EnsureSuccess(response, responsePayload);
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, int maximumBytes, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > maximumBytes) throw new ControllerCommunicationException("Controller response is too large.", response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken); if (read == 0) break;
            if (buffer.Length + read > maximumBytes) throw new ControllerCommunicationException("Controller response is too large.", response.StatusCode);
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static void EnsureSuccess(HttpResponseMessage response, byte[] payload)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = payload.Length == 0 ? response.ReasonPhrase : Encoding.UTF8.GetString(payload);
        if (detail?.Length > 512) detail = detail[..512];
        throw new ControllerCommunicationException($"Controller returned {(int)response.StatusCode}: {detail}", response.StatusCode);
    }
}

public sealed class ControllerCommunicationException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed record DiscoveredDevice(string DeviceId, string HostName, IPAddress Address, int Port,
    int ProtocolVersion, string FirmwareVersion)
{
    public Uri BaseAddress => new($"http://{Address}:{Port}/");
    public string MdnsName => $"{HostName}.local";
}

public static class DiscoveryProtocol
{
    public const string Query = "ATLAS_DISCOVER/1";
    public const string MdnsService = "_atlasled._tcp.local";
    public const int DefaultPort = 45454;

    public static byte[] EncodeAnnouncement(DiscoveredDevice device)
    {
        Validate(device);
        return Encoding.UTF8.GetBytes($"ATLAS_DEVICE/1|{device.DeviceId}|{device.HostName}|{device.Address}|{device.Port}|{device.ProtocolVersion}|{device.FirmwareVersion}");
    }

    public static DiscoveredDevice DecodeAnnouncement(ReadOnlySpan<byte> payload)
    {
        if (payload.Length is 0 or > 1024) throw new ProtocolException("Discovery payload size is invalid.");
        var parts = Encoding.UTF8.GetString(payload).Split('|');
        if (parts.Length != 7 || parts[0] != "ATLAS_DEVICE/1" || !IPAddress.TryParse(parts[3], out var address) ||
            !int.TryParse(parts[4], out var port) || !int.TryParse(parts[5], out var version))
            throw new ProtocolException("Discovery announcement is invalid.");
        var device = new DiscoveredDevice(parts[1], parts[2], address, port, version, parts[6]);
        Validate(device); ProtocolJson.ValidateVersion(version);
        return device;
    }

    private static void Validate(DiscoveredDevice device)
    {
        if (string.IsNullOrWhiteSpace(device.DeviceId) || device.DeviceId.Length > 128 ||
            string.IsNullOrWhiteSpace(device.HostName) || device.HostName.Length > 63 ||
            device.HostName.Any(character => !(char.IsLetterOrDigit(character) || character == '-')) ||
            device.HostName.StartsWith('-') || device.HostName.EndsWith('-') || device.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(device.FirmwareVersion) || device.FirmwareVersion.Length > 64)
            throw new ProtocolException("Discovery announcement fields are invalid.");
    }
}

public sealed class UdpLanDiscovery(int port = DiscoveryProtocol.DefaultPort)
{
    public async ValueTask<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var client = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        var query = Encoding.ASCII.GetBytes(DiscoveryProtocol.Query);
        await client.SendAsync(query, new IPEndPoint(IPAddress.Broadcast, port), cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var devices = new Dictionary<string, DiscoveredDevice>(StringComparer.OrdinalIgnoreCase);
        while (!timeoutSource.IsCancellationRequested)
        {
            try
            {
                var response = await client.ReceiveAsync(timeoutSource.Token);
                var device = DiscoveryProtocol.DecodeAnnouncement(response.Buffer);
                devices[device.DeviceId] = device with { Address = response.RemoteEndPoint.Address };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { break; }
            catch (ProtocolException) { }
        }
        return devices.Values.OrderBy(device => device.HostName, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

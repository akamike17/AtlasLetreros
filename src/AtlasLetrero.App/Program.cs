using AtlasLetrero.App.Services;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5187");
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddSingleton<AppPaths>();
builder.Services.AddSingleton<AtomicFileWriter>();
builder.Services.AddSingleton<ProjectPackageService>();
builder.Services.AddSingleton<ProjectIndexService>();
builder.Services.AddSingleton<AssetCatalogService>();
builder.Services.AddSingleton<DeviceProtocolService>();
builder.Services.AddSingleton<DeviceConnectionService>();
builder.Services.AddSingleton<SerialDiscoveryService>();
builder.Services.AddSingleton<DeploymentService>();
var app = builder.Build();
app.Use(async (context, next) => {
    try { await next(); }
    catch (Exception ex) {
        app.Logger.LogError(ex, "Solicitud fallida");
        context.Response.StatusCode = ex is FileNotFoundException ? 404 : ex is ArgumentException or InvalidDataException ? 400 : 503;
        await context.Response.WriteAsJsonAsync(new { code = ex is TimeoutException ? "DEVICE_TIMEOUT" : "REQUEST_FAILED", message = ex is TimeoutException ? "No se recibió respuesta del letrero." : ex is ArgumentException or InvalidDataException or FileNotFoundException ? ex.Message : "No se pudo completar la operación. El archivo original no fue modificado.", details = (string?)null });
    }
});
app.UseDefaultFiles(); app.UseStaticFiles(); app.MapControllers();
app.Run();

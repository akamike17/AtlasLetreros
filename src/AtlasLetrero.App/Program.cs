using AtlasLetrero.App.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.WebHost.UseUrls("http://127.0.0.1:5088");
builder.Services.AddSingleton<AppPaths>();
builder.Services.AddSingleton<AtomicFileWriter>();
builder.Services.AddSingleton<ProjectPackageService>();
builder.Services.AddSingleton<DeviceConnectionService>();
builder.Services.AddControllers();
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error procesando {Path}", context.Request.Path);
        context.Response.StatusCode = ex is FileNotFoundException ? 404 : ex is ArgumentException or InvalidDataException ? 400 : 500;
        await context.Response.WriteAsJsonAsync(new { code = "REQUEST_FAILED", message = context.Response.StatusCode == 404 ? "No se encontró el proyecto." : context.Response.StatusCode == 400 ? "Los datos del proyecto no son válidos." : "No se pudo completar la operación. El archivo original no fue modificado.", details = (string?)null });
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

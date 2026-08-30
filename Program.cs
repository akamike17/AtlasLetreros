using Microsoft.AspNetCore.DataProtection;
using AtlasLetrero.Application;
using AtlasLetrero.Domain;
using AtlasLetrero.Simulator;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".runtime", "keys"));
builder.Services.AddDataProtection().PersistKeysToFileSystem(keyDirectory);

// Add services to the container.
builder.Services.AddControllersWithViews();
var topology = new MatrixTopology(32, 16,
    [new MatrixTile(0, 0, 32, 16, Layout: MatrixLayout.Serpentine)], ChannelOrder.Grb);
var simulator = new SimulatorDevice(new DeviceConfiguration("atlas-simulator", topology, 184));
await simulator.BootAsync();
builder.Services.AddSingleton(simulator);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

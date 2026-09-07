using Microsoft.Playwright;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using static Microsoft.Playwright.Assertions;

var url = args[0];
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = true,
    ExecutablePath = Environment.GetEnvironmentVariable("ATLAS_TEST_CHROME")
});
await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1920, Height = 1080 }, BypassCSP = true });
await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
var page = await context.NewPageAsync();
page.SetDefaultTimeout(10000);
var errors = new List<string>();
page.PageError += (_, message) => errors.Add(message);
page.Console += (_, message) => { if (message.Type == "error" && !message.Text.Contains("400") && !message.Text.Contains("503")) errors.Add(message.Text); };
var checks = new List<string>();
void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks.Add(label); Console.WriteLine("PASS: " + label); }
ILocator Button(string name) => page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
async Task Value(string label, string value)
{
    var input = page.GetByLabel(label, new() { Exact = true });
    await input.FillAsync(value);
    await input.PressAsync("Tab");
}
async Task VisiblePixels(string label, bool requireSimulator = false)
{
    var condition = requireSimulator ? "() => { const a=document.querySelector('#design-canvas')?.dataset.pixels,b=document.querySelector('#simulator-canvas')?.dataset.pixels; return a && a===b && a.split(',').some((v,i)=>i%4!==3&&+v>0); }" : "() => { const a=document.querySelector('#design-canvas')?.dataset.pixels; return a && a.split(',').some((v,i)=>i%4!==3&&+v>0); }";
    await page.WaitForFunctionAsync(condition);
    Check(true, label);
}
async Task InsertText(string text, string effect)
{
    await Button("Texto").ClickAsync();
    await page.Locator("dialog").GetByLabel("Texto", new() { Exact = true }).FillAsync(text);
    await page.Locator("dialog").GetByLabel("Fuente LED", new() { Exact = true }).SelectOptionAsync("fonts/5x7.json");
    await page.Locator("dialog").GetByLabel("Escala", new() { Exact = true }).FillAsync("1");
    await page.Locator("dialog").GetByLabel("Efecto", new() { Exact = true }).SelectOptionAsync(effect);
    await Button("Aceptar").ClickAsync();
    await Expect(page.Locator("dialog")).ToHaveCountAsync(0);
}
try
{
    await page.GotoAsync(url);
    await Button("+ Nuevo proyecto").ClickAsync();
    await page.GetByLabel("Nombre", new() { Exact = true }).FillAsync("SOLUCIONES MG — prueba automática");
    await page.GetByLabel("Ancho (LED)", new() { Exact = true }).FillAsync("96");
    await Button("Crear").ClickAsync();
    await page.WaitForURLAsync("**/editor.html?id=*");
    var editorUrl = page.Url;
    var id = new Uri(editorUrl).Query.Split('=')[1];
    await Expect(page.Locator("#design-canvas")).ToBeVisibleAsync();
    await Button("Texto").ClickAsync();
    await page.Locator("dialog").GetByLabel("Texto", new() { Exact = true }).FillAsync("SOLUCIONES MG");
    await page.Locator("dialog").GetByLabel("Fuente LED", new() { Exact = true }).SelectOptionAsync("fonts/5x7.json");
    await page.Locator("dialog").GetByLabel("Escala", new() { Exact = true }).FillAsync("6");
    await Button("Aceptar").ClickAsync();
    await Expect(page.Locator("dialog [data-submit-error]")).ToContainTextAsync("matriz tiene 16");
    await page.Locator("dialog").GetByLabel("Escala", new() { Exact = true }).FillAsync("5");
    await Expect(page.Locator("dialog .full[role=status]")).ToContainTextAsync("matriz tiene 16");
    Check(true, "La escala inválida se rechaza y el aviso sigue funcionando tras reintentar");
    await page.Locator("dialog").GetByLabel("Escala", new() { Exact = true }).FillAsync("1");
    await page.Locator("dialog").GetByLabel("Efecto", new() { Exact = true }).SelectOptionAsync("blink");
    await Button("Aceptar").ClickAsync();
    await Expect(page.Locator("dialog")).ToHaveCountAsync(0);
    await Value("X", "9");
    await Value("Duración del frame (ms)", "10000");
    await VisiblePixels("Encabezado visible en el Canvas");

    await Button("Ocultar Capa 1").ClickAsync();
    await page.WaitForFunctionAsync("() => document.querySelector('#design-canvas').dataset.pixels.split(',').every((v,i)=>i%4===3||+v===0)");
    await Button("Deshacer").ClickAsync();
    await VisiblePixels("Deshacer restaura la capa");
    await Button("Rehacer").ClickAsync();
    await Button("Mostrar Capa 1").ClickAsync();
    await VisiblePixels("Rehacer y mostrar capa funcionan");
    await Button("Reproducir").ClickAsync();
    await page.WaitForFunctionAsync("() => document.querySelector('.time').textContent !== '0.00 / 10.00 s'");
    await Button("Pausar").ClickAsync();
    await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    var paused = await page.Locator(".time").TextContentAsync();
    await Task.Delay(150);
    Check(paused == await page.Locator(".time").TextContentAsync(), "Pausar conserva el instante");
    await page.Locator(".playback").GetByRole(AriaRole.Button, new() { Name = "Detener", Exact = true }).ClickAsync();
    await Expect(page.Locator(".time")).ToHaveTextAsync("0.00 / 10.00 s");
    await Value("Duración del frame (ms)", "");
    Check(!await page.GetByLabel("Duración del frame (ms)").EvaluateAsync<bool>("e=>e.checkValidity()"), "Duración vacía rechazada");
    await Value("Duración del frame (ms)", "10000");

    await Button("+ Frame").ClickAsync();
    await InsertText("SE REPARAN COMPUTADORAS", "left");
    await Value("Y", "4");
    await Value("Duración del frame (ms)", "6000");
    await VisiblePixels("Texto lineal visible al comenzar el segundo frame");
    await Button("+ Frame").ClickAsync();
    await Button("Icono").ClickAsync();
    await page.GetByLabel("Buscar recursos").FillAsync("Computadora");
    await page.Locator("dialog .asset-grid").GetByRole(AriaRole.Button, new() { Name = "Computadora", Exact = true }).ClickAsync();
    await page.GetByLabel("Tamaño (LED)").FillAsync("16");
    await Button("Aceptar").ClickAsync();
    await Expect(page.Locator("dialog")).ToHaveCountAsync(0);
    await Value("X", "40");
    await Value("Duración del frame (ms)", "3000");
    await VisiblePixels("Icono de PC visible en el tercer frame");
    await Button("Guardar").ClickAsync();
    await Expect(page.Locator(".save-state")).ToHaveTextAsync("Guardado");
    await page.ReloadAsync();
    await Expect(Button("Seleccionar frame 3")).ToBeVisibleAsync();
    await VisiblePixels("Reabrir conserva los objetos del proyecto");
    await Button("Seleccionar frame 3").ClickAsync();
    await VisiblePixels("Reabrir conserva el icono");
    await Button("Seleccionar frame 1").ClickAsync();

    // Save can still be running when the user opens Configuration.
    var entered = new TaskCompletionSource();
    var release = new TaskCompletionSource();
    var delayedOnce = false;
    await page.RouteAsync("**/api/projects/" + id, async route =>
    {
        if (route.Request.Method == "PUT" && !delayedOnce)
        {
            delayedOnce = true; entered.TrySetResult();
            await release.Task;
        }
        await route.ContinueAsync();
    });
    await Value("Nombre del proyecto", "SOLUCIONES MG — guardado durante navegación");
    await Button("Guardar").ClickAsync();
    await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
    await page.GetByRole(AriaRole.Link, new() { Name = "Configuración", Exact = true }).ClickAsync();
    release.TrySetResult();
    await page.WaitForURLAsync("**/device.html?id=*");
    await page.UnrouteAsync("**/api/projects/" + id);
    await Expect(Button("Guardar configuración")).ToBeEnabledAsync();
    await page.GetByRole(AriaRole.Link, new() { Name = "Volver", Exact = true }).ClickAsync();
    await Expect(page.GetByLabel("Nombre del proyecto")).ToHaveValueAsync("SOLUCIONES MG — guardado durante navegación");
    Check(true, "Configuración abre y espera al guardado en curso sin perder cambios");

    await page.RouteAsync("**/api/projects/" + id, async route =>
    {
        if (route.Request.Method == "PUT") await route.FulfillAsync(new() { Status = 503, ContentType = "application/json", Body = "{\"message\":\"Disco ocupado (prueba)\"}" });
        else await route.ContinueAsync();
    });
    await Value("Nombre del proyecto", "SOLUCIONES MG — prueba de reintento");
    await Button("Guardar").ClickAsync();
    await Expect(page.Locator(".toast.error")).ToContainTextAsync("Disco ocupado");
    await Expect(page.Locator(".save-state")).ToHaveTextAsync("Sin guardar");
    await page.UnrouteAsync("**/api/projects/" + id);
    await Button("Guardar").ClickAsync();
    await Expect(page.Locator(".save-state")).ToHaveTextAsync("Guardado");
    Check(true, "Guardado fallido conserva cambios y permite reintentar");

    await Button("Enviar al simulador").ClickAsync();
    await Expect(page.Locator("#deployment-status")).ToHaveTextAsync("Correcto", new() { Timeout = 30000 });
    await VisiblePixels("Paquete recibido coincide con el Canvas", true);
    Check((await page.Locator("#checksum").GetAttributeAsync("title"))?.Length == 64, "SHA-256 recibido y verificado");
    await Expect(Button("Cancelar envío")).ToBeHiddenAsync();
    foreach (var size in new[] { (1920, 1080), (1600, 900), (1366, 768) })
    {
        await page.SetViewportSizeAsync(size.Item1, size.Item2);
        await Expect(page.Locator("#simulator-canvas")).ToBeInViewportAsync();
        await Expect(Button("Enviar al simulador")).ToBeInViewportAsync();
        var frameButton = await Button("Seleccionar frame 3").BoundingBoxAsync();
        var frameStrip = await page.Locator(".frames").BoundingBoxAsync();
        Check(frameButton is not null && frameStrip is not null && frameButton.Height >= 28 && frameStrip.Height >= frameButton.Height && frameButton.Y >= frameStrip.Y && frameButton.Y + frameButton.Height <= frameStrip.Y + frameStrip.Height + 1, $"Selector de frames completo a {size.Item1}×{size.Item2}");
        Check(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth<=innerWidth && document.documentElement.scrollHeight<=innerHeight"), $"Editor sin desbordamiento a {size.Item1}×{size.Item2}");
        await page.ScreenshotAsync(new() { Path = Path.Combine(output, $"editor-{size.Item1}.png") });
    }
    using var client = new HttpClient { BaseAddress = new Uri(url) };
    var project = JsonNode.Parse(await client.GetStringAsync("/api/projects/" + id))!.AsObject();
    var savedFrames = project["scenes"]![0]!["frames"]!.AsArray();
    Check(savedFrames.Count == 3 && savedFrames[0]!["durationMs"]!.GetValue<int>() == 10000 && savedFrames[1]!["durationMs"]!.GetValue<int>() == 6000 && savedFrames[2]!["durationMs"]!.GetValue<int>() == 3000, "Persistencia conserva la secuencia 10 s / 6 s / 3 s");
    var before = project.ToJsonString();
    savedFrames[0]!["durationMs"] = 0;
    var invalid = await client.PutAsJsonAsync("/api/projects/" + id, project);
    Check((int)invalid.StatusCode == 400, "API rechaza duración inválida");
    Check(before == JsonNode.Parse(await client.GetStringAsync("/api/projects/" + id))!.ToJsonString(), "Guardado inválido no modifica el archivo anterior");
    Check(errors.Count == 0, "Sin errores JavaScript ni fallos inesperados de consola");
    await File.WriteAllTextAsync(Path.Combine(output, "result.json"), System.Text.Json.JsonSerializer.Serialize(new { status = "PASS", checks, errors }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
}
catch (Exception ex)
{
    await page.ScreenshotAsync(new() { Path = Path.Combine(output, "failure.png"), FullPage = true });
    await File.WriteAllTextAsync(Path.Combine(output, "failure.txt"), ex + "\n" + string.Join("\n", errors));
    throw;
}
finally { await context.Tracing.StopAsync(new() { Path = Path.Combine(output, "trace.zip") }); }

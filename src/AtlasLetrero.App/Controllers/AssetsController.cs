using Microsoft.AspNetCore.Mvc;
using AtlasLetrero.App.Services;
namespace AtlasLetrero.App.Controllers;
[ApiController, Route("api/assets")] public class AssetsController(AssetCatalogService assets) : ControllerBase {
    [HttpGet("icons")] public object Icons(string? q) => assets.Search("icons", q);
    [HttpGet("emojis")] public object Emojis(string? q) => assets.Search("emojis", q);
    [HttpGet("fonts")] public object Fonts() => assets.Search("fonts", null);
}

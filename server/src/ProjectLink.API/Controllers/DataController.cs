using Microsoft.AspNetCore.Mvc;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly IStaticDataService _staticData;

    public DataController(IStaticDataService staticData) => _staticData = staticData;

    [HttpGet("bundle")]
    public IActionResult GetBundle()
    {
        var bundle = _staticData.GetClientBundle();
        if (string.IsNullOrEmpty(bundle))
            return NotFound(new { reason = "bundle_not_generated" });
        return Content(bundle, "application/json");
    }
}

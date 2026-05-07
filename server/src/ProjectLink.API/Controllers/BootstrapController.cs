using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Bootstrap;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/bootstrap")]
public class BootstrapController : ControllerBase
{
    private readonly BootstrapService _bootstrap;

    public BootstrapController(BootstrapService bootstrap) => _bootstrap = bootstrap;

    [HttpGet("config")]
    public IActionResult GetConfig() => Ok(_bootstrap.Get());
}

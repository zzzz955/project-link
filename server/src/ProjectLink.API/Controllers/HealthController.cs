using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLink.Infrastructure.Persistence;
using StackExchange.Redis;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext          _db;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbStatus    = "ok";
        var redisStatus = "ok";

        try { await _db.Database.ExecuteSqlRawAsync("SELECT 1"); }
        catch { dbStatus = "error"; }

        try { await _redis.GetDatabase().PingAsync(); }
        catch { redisStatus = "error"; }

        return Ok(new { db = dbStatus, redis = redisStatus });
    }
}

using Microsoft.AspNetCore.Mvc;
using ProjectLink.Contracts.Event;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IStaticDataService _staticData;

    public EventController(IStaticDataService staticData) => _staticData = staticData;

    [HttpGet("season")]
    public IActionResult GetSeasonEvents()
    {
        var now    = DateTimeOffset.UtcNow;
        var events = _staticData.GetAllSeasonEvents()
            .Select(e =>
            {
                var isActive = DateTimeOffset.TryParse(e.StartAt, out var start) &&
                               DateTimeOffset.TryParse(e.EndAt,   out var end)   &&
                               now >= start && now < end;
                return new SeasonEventEntry
                {
                    EventId     = e.EventId,
                    Name        = e.Name,
                    Type        = e.Type,
                    StartAt     = e.StartAt,
                    EndAt       = e.EndAt,
                    MetricLabel = e.MetricLabel,
                    IsActive    = isActive,
                    IsLocked    = !isActive,
                };
            })
            .ToList();

        return Ok(new ActiveEventsResponse { Events = events });
    }
}

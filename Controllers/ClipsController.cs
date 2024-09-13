using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class ClipsController : ControllerBase
{
    private readonly ITwitchService _twitchService;

    public ClipsController(ITwitchService twitchService)
    {
        _twitchService = twitchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClips([FromQuery] DateTime start_date, [FromQuery] DateTime end_date)
    {
        try
        {
            var clips = await _twitchService.FetchClips(start_date, end_date);
            return Ok(clips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
    
}
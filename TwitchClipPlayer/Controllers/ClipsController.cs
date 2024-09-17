using Microsoft.AspNetCore.Mvc;
using TwitchClipPlayer.Services;


namespace TwitchClipPlayer.Controllers;

    [ApiController]
    [Route("[controller]")]
    public class ClipsController(ITwitchService twitchService) : ControllerBase
    {
        private readonly ITwitchService _twitchService =
            twitchService ?? throw new ArgumentNullException(nameof(twitchService));

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

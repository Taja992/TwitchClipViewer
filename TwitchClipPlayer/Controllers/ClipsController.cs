using Microsoft.AspNetCore.Mvc;
using TwitchClipPlayer.Services;

namespace TwitchClipPlayer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClipsController : ControllerBase
    {
        private readonly ITwitchService _twitchService;

        public ClipsController(ITwitchService twitchService)
        {
            _twitchService = twitchService ?? throw new ArgumentNullException(nameof(twitchService));
        }

        [HttpGet]
        public async Task<IActionResult> GetClips([FromQuery] DateTime start_date, [FromQuery] DateTime end_date, [FromQuery] string broadcaster_name)
        {
            try
            {
                var broadcasterId = await _twitchService.GetChannelIdByName(broadcaster_name);
                var clips = await _twitchService.FetchClips(start_date, end_date, broadcasterId);
                return Ok(clips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
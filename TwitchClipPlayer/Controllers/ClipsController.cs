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
        public async Task<IActionResult> GetClips([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string broadcasterName)
        {
            try
            {
                var broadcasterId = await _twitchService.GetChannelIdByName(broadcasterName);
                var clips = await _twitchService.FetchClips(startDate, endDate, broadcasterId);
                return Ok(clips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
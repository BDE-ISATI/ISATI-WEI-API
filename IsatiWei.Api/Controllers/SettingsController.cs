using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IsatiWei.Api.Models;
using IsatiWei.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IsatiWei.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _settingsService;

        public SettingsController(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Get the game settings
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<GameSettings>> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();

            return Ok(settings);
        }

        /// <summary>
        /// Toggle the users ranking visibility
        /// </summary>
        /// <returns></returns>
        [HttpPut("admin_update/toggle_users_ranking_visibility")]
        public async Task<IActionResult> ToggleUsersRankingVisiblity()
        {
            await _settingsService.ToggleUsersRankingVisibility();

            return Ok();
        }

        /// <summary>
        /// Toggle the teams ranking visibility
        /// </summary>
        /// <returns></returns>
        [HttpPut("admin_update/toggle_teams_ranking_visibility")]
        public async Task<IActionResult> ToggleTeamsRankingVisiblity()
        {
            await _settingsService.ToggleTeamsRankingVisibility();

            return Ok();
        }
    }
}

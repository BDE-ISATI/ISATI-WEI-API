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
    public class TeamsController : ControllerBase
    {
        private readonly TeamService _teamService;

        public TeamsController(TeamService teamService)
        {
            _teamService = teamService;
        }

        /*
         * Get
         */

        /// <summary>
        /// Get a team based on its ID
        /// </summary>
        /// <param name="id">The id of the team</param>
        /// <returns>The team corresponding on the ID</returns>
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Team>> GetTeamAsync(string id)
        {
            Team team = await _teamService.GetTeamAsync(id);

            if (team == null)
            {
                return NotFound();
            }

            return team;
        }

        /// <summary>
        /// Get all teams
        /// </summary>
        /// <returns>A List of all teams</returns>
        [HttpGet]
        public async Task<ActionResult<List<Team>>> GetTeamsAsync()
        {
            List<Team> teams = await _teamService.GetTeamsAsync();

            return teams;
        }

        /// <summary>
        /// Get team rank
        /// </summary>
        /// <param name="id">The team ID</param>
        /// <returns>The team rank</returns>
        [HttpGet("{id:length(24)}/rank")]
        public async Task<ActionResult<int>> GetTeamRankAsync(string id)
        {
            return await _teamService.GetTeamRankAsync(id);
        }

        /// <summary>
        /// Get the team of a specific user
        /// </summary>
        /// <param name="userId">The user's ID we want to know the team</param>
        /// <returns></returns>
        [HttpGet("for_user/{userId:length(24)}")]
        public async Task<ActionResult<Team>> GetTeamForUser(string userId)
        {
            Team team = await _teamService.GetUserTeamAsync(userId);

            return Ok(team);
        }

        /*
         * Post
         */
        /// <summary>
        /// Add new team
        /// </summary>
        /// <remarks>
        /// The data to create the team are passed on the body
        /// It should look like that:
        /// 
        ///      POST /add
        ///      {
        ///           "name": "my team name",
        ///           "captainId": "the captain ID"
        ///      }
        /// </remarks>
        /// <param name="team"></param>
        /// <returns>The newly created team</returns>
        [HttpPost("add")]
        public async Task<ActionResult<Team>> AddTeam([FromBody] Team team)
        {
            try
            {
                team = await _teamService.CreateTeamAsyn(team.Name, team.CaptainId);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return team;
        }

        /*
         * Put
         */
        /// <summary>
        /// Update a team
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        [HttpPut("admin_update")]
        public async Task<IActionResult> UpdateTeam([FromBody] Team team)
        {
            try
            {
                await _teamService.UpdateTeamAsync(team);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Add a user to a team
        /// </summary>
        /// <remarks>
        /// Here the ID is enough
        /// 
        ///      PUT /{teamId}/add_user
        ///      {
        ///           "id": "5f1aa9610cb5aa794889bdd4"
        ///      }
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="userToAdd"></param>
        /// <returns></returns>
        [HttpPut("{id:length(24)}/add_user")]
        public async Task<IActionResult> AddUserToTeam(string id, [FromBody] User userToAdd)
        {
            try
            {
                await _teamService.AddUserToTeam(id, userToAdd.Id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /*
         * Delete
         */
        /// <summary>
        /// Delete a team
        /// </summary>
        /// <param name="id">The ID of the team you want to delete</param>
        /// <returns></returns>
        [HttpDelete("delete/{id:length(24)}")]
        public async Task<IActionResult> DeleteTeam(string id)
        {
            bool exist = (await _teamService.GetTeamAsync(id)) != null;

            if (!exist)
            {
                return NotFound();
            }

            await _teamService.DeleteTeamAsync(id);

            return Ok();
        }
    }
}

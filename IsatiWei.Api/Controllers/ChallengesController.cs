using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Game;
using IsatiWei.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IsatiWei.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengesController : ControllerBase
    {
        private readonly ChallengeService _challengeService;

        public ChallengesController(ChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        /*
         * Get
         */
        /// <summary>
        /// Get a challenge based on its ID
        /// </summary>
        /// <param name="id">The ID of the challenge</param>
        /// <returns>The challenge with the given ID</returns>
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Challenge>> GetChallenge(string id)
        {
            var challenge = await _challengeService.GetChallengeAsync(id);

            if (challenge == null)
            {
                return NotFound();
            }

            return Ok(challenge);
        }

        /// <summary>
        /// Get a list of all challenges
        /// </summary>
        /// <returns>The challenges</returns>
        [HttpGet]
        public async Task<ActionResult<List<Challenge>>> GetChallenges()
        {
            var challenges = await _challengeService.GetChallengesAsync();

            return Ok(challenges);
        }

        /// <summary>
        /// Get a list of all challenges for the player
        /// </summary>
        /// <returns>The challenges</returns>
        [HttpGet("individual/{player:length(24)}")]
        public async Task<ActionResult<List<IndividualChallenge>>> GetChallengeForPlayer(string player)
        {
            var challenges = await _challengeService.GetChallengeForPlayerAsync(player);

            return Ok(challenges);
        }

        /// <summary>
        /// Get a list of all challenges for the team
        /// </summary>
        /// <returns>The challenges</returns>
        [HttpGet("team/{team:length(24)}")]
        public async Task<ActionResult<List<Challenge>>> GetTeamsChallenges(string team)
        {
            var challenges = await _challengeService.GetChallengeForTeamAsync(team);

            return Ok(challenges);
        }

        /// <summary>
        /// Get challenges waiting for validation
        /// </summary>
        /// <remarks>
        /// Only captain and admin can do this, meaning the ID is determined with authorization headers
        /// </remarks>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpGet("waiting")]
        public async Task<ActionResult<List<WaitingChallenge>>> GetWaitingChallenges([FromHeader] string authorization)
        {
            List<WaitingChallenge> result = await _challengeService.GetWaitingChallenges(UserIdFromAuth(authorization));

            return Ok(result);
        }

        /*
         * Post
         */

        /// <summary>
        /// Add a new challenge
        /// </summary>
        /// <remarks>
        /// The image must be encoded in a base64 string
        /// </remarks>
        /// <param name="toCreate"></param>
        /// <returns></returns>
        [HttpPost("add")]
        public async Task<IActionResult> CreateChallenge([FromBody] Challenge toCreate)
        {
            try
            {
                await _challengeService.CreateChallengeAsync(toCreate);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Submit a challenge for validation
        /// </summary>
        /// <param name="toSubmit"></param>
        /// <returns></returns>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitChallengeForValidation([FromBody] WaitingChallenge toSubmit)
        {
            try
            {
                await _challengeService.SubmitChallengeForValidationAsync(toSubmit.ValidatorId, toSubmit.Id, Convert.FromBase64String(toSubmit.Base64ProofImage));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Validate a challenge for a user
        /// </summary>
        /// <remarks>
        /// In this particulare case, their is no need to add the proof image to the JSON
        /// </remarks>
        /// <param name="toValidate"></param>
        /// <returns></returns>
        [HttpPost("validate_for_user")]
        public async Task<IActionResult> ValidateChallengeForUser([FromBody] WaitingChallenge toValidate)
        {
            try
            {
                await _challengeService.ValidateChallengeForUserAsync(toValidate.ValidatorId, toValidate.Id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Validate a challenge for a team
        /// </summary>
        /// <remarks>
        /// In this particulare case, their is no need to add the proof image to the JSON
        /// </remarks>
        /// <param name="toValidate"></param>
        /// <returns></returns>
        [HttpPost("validate_for_team")]
        public async Task<IActionResult> ValidateChallengeForTeam([FromBody] WaitingChallenge toValidate)
        {
            try
            {
                await _challengeService.ValidateChallengeForTeamAsync(toValidate.ValidatorId, toValidate.Id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /*
         * Put
         */
        /// <summary>
        /// Update the challenge
        /// </summary>
        /// <param name="id">The challenge's ID you want to update</param>
        /// <param name="toUpdate"></param>
        /// <returns>Not found if the challenge ID doesn't exist, Ok otherwise</returns>
        [HttpPut("update/{id:length(24)}")]
        public async Task<IActionResult> UpdateChallenge(string id, [FromBody] Challenge toUpdate)
        {
            bool exist = (await _challengeService.GetChallengeAsync(id)) != null;

            if (!exist)
            {
                return NotFound();
            }

            try
            {
                await _challengeService.UpdateChallengeAsync(id, toUpdate);
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
        public async Task<IActionResult> DeleteChallenge(string id)
        {
            bool exist = (await _challengeService.GetChallengeAsync(id)) != null;

            if (!exist)
            {
                return NotFound();
            }

            await _challengeService.DeleteChallengeAsync(id);

            return Ok();
        }

        /*
         * Utility
         */
        private string UserIdFromAuth(string authorization)
        {
            string encodedUsernamePassword = authorization.Substring("Basic ".Length).Trim();
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string idAndPassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

            int seperatorIndex = idAndPassword.IndexOf(':');

            var userId = idAndPassword.Substring(0, seperatorIndex);

            return userId;
        }
    }
}
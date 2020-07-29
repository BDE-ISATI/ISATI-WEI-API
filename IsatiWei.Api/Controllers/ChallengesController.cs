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
            List<WaitingChallenge> result = await _challengeService.GetWaitingChallenges(UserUtilities.UserIdFromAuth(authorization));

            return Ok(result);
        }

        /// <summary>
        /// Get all challenges realised at least on time by player
        /// </summary>
        /// <param name="player">The player we want done challenges</param>
        /// <returns></returns>
        [HttpGet("done/{player:length(24)}")]
        public async Task<ActionResult<List<IndividualChallenge>>> GetDoneChallenges(string player)
        {
            var result = await _challengeService.GetDoneChallenges(player);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Get the challenge image
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:length(24)}/image")]
        public async Task<ActionResult<ChallengeImage>> GetChallengeImage(string id)
        {
            var result = await _challengeService.GetChallengeImage(id);

            if (result == null)
            {
                return NoContent();
            }

            return Ok(new ChallengeImage()
            {
                Image = result
            });
        }

        /// <summary>
        /// Get the proof image for a challenge for a player
        /// </summary>
        /// <param name="id"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        [HttpGet("{id:length(24)}/proof/{player:length(24)}")]
        public async Task<ActionResult<ChallengeProofImage>> GetProofImage(string id, string player)
        {
            var result = await _challengeService.GetProofImage(id, player);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(new ChallengeProofImage()
            {
                Image = result
            });
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
        public async Task<ActionResult<Challenge>> CreateChallenge([FromBody] Challenge toCreate)
        {
            try
            {
                toCreate = await _challengeService.CreateChallengeAsync(toCreate);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok(toCreate);
        }

        /// <summary>
        /// Submit a challenge for validation
        /// </summary>
        /// <remarks>
        /// You only need a few fields here
        /// 
        ///     POST /submit
        ///     {
        ///         "proofImage": "Base64 string of the proof image"
        ///     }
        /// </remarks>
        /// <param name="toSubmit"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitChallengeForValidation([FromBody] ChallengeSubmission toSubmit, [FromHeader] string authorization)
        {
            try
            {
                await _challengeService.SubmitChallengeForValidationAsync(UserUtilities.UserIdFromAuth(authorization), toSubmit.ChallengeId, toSubmit.ProofImage);
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
        /// 
        ///     POST /validate_for_user
        ///     {
        ///         "validatorId": "Id of the player"
        ///     }
        /// </remarks>
        /// <param name="toValidate"></param>
        /// <returns></returns>
        [HttpPost("validate_for_user")]
        public async Task<IActionResult> ValidateChallengeForUser([FromBody] ChallengeSubmission toValidate)
        {
            try
            {
                await _challengeService.ValidateChallengeForUserAsync(toValidate.ValidatorId,toValidate.ChallengeId);
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
        /// 
        ///     POST /validate_for_team
        ///     {
        ///         "validatorId": "Id of the team"
        ///     }
        /// </remarks>
        /// <param name="toValidate"></param>
        /// <returns></returns>
        [HttpPost("validate_for_team")]
        public async Task<IActionResult> ValidateChallengeForTeam([FromBody] ChallengeSubmission toValidate)
        {
            try
            {
                await _challengeService.ValidateChallengeForTeamAsync(toValidate.ValidatorId, toValidate.ChallengeId);
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
        /// <param name="toUpdate"></param>
        /// <returns>Not found if the challenge ID doesn't exist, Ok otherwise</returns>
        [HttpPut("admin_update")]
        public async Task<IActionResult> UpdateChallenge([FromBody] Challenge toUpdate)
        {
            bool exist = (await _challengeService.GetChallengeAsync(toUpdate.Id)) != null;

            if (!exist)
            {
                return NotFound();
            }

            try
            {
                await _challengeService.UpdateChallengeAsync(toUpdate);
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
        /// Delete a challenge
        /// </summary>
        /// <param name="id">The ID of the challenge you want to delete</param>
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
    }
}
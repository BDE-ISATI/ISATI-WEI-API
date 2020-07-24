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
    }
}
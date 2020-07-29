using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Authentication;
using IsatiWei.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IsatiWei.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get a list of all challenges
        /// </summary>
        /// <returns>The challenges</returns>
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            var users = await _userService.GetUsersAsync();

            return Ok(users);
        }

        /// <summary>
        /// Get the user profile picture
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:length(24)}/profile_picture")]
        public async Task<ActionResult<UserProfilePicture>> GetProfilePicture(string id)
        {
            var profilePicture = await _userService.GetProfilePicture(id);

            if (profilePicture == null)
            {
                return NoContent();
            }

            return Ok(new UserProfilePicture()
            {
                ProfilePicture = profilePicture
            });
        }

        /// <summary>
        /// Update the profile picture for user
        /// </summary>
        /// <param name="profilePictureUpdate"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPut("update/profile_picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UserProfilePicture profilePictureUpdate, [FromHeader] string authorization)
        {
            try
            {
                await _userService.UpdateProfilePicture(UserUtilities.UserIdFromAuth(authorization), profilePictureUpdate.ProfilePicture);
            } 
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Update a user
        /// </summary>
        /// <param name="toUpdate"></param>
        /// <returns></returns>
        [HttpPut("admin_update")]
        public async Task<IActionResult> UpdateUser([FromBody] User toUpdate)
        {
            try
            {
                await _userService.UpdateUserAsync(toUpdate);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }
    }
}

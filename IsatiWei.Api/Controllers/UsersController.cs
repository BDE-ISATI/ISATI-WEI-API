using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Update the profile picture for user
        /// </summary>
        /// <param name="profilePictureUpdate"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        [HttpPut("update/profile_picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UserProfilePictureUpdate profilePictureUpdate, [FromHeader] string authorization)
        {
            try
            {
                await _userService.UpdateProfilePicture(UserIdFromAuth(authorization), profilePictureUpdate.ProfilePicture);
            } 
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

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

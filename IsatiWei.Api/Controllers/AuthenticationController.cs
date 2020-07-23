using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Authentication;
using IsatiWei.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IsatiWei.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationService _authenticationService;
        private readonly IMapper _mapper;

        public AuthenticationController(AuthenticationService authenticationService, IMapper mapper)
        {
            _authenticationService = authenticationService;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody]UserRegister registerModel)
        {
            var user = _mapper.Map<User>(registerModel);

            try
            {
                _authenticationService.Register(user, registerModel.Password);
            } 
            catch (Exception e)
            {
                return BadRequest($"Can't regster the user: {e.Message}");
            }

            return Ok();
        }

        [HttpPost("login")]
        public ActionResult<User> Login([FromBody]UserLogin loginModel)
        {
            var user = _authenticationService.Login(loginModel.Username, loginModel.Password);

            if (user == null)
            {
                return BadRequest("Username or password is incorrect");
            }

            return Ok(user);
        }
    }
}

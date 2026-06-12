using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IAuthService _authService;

        public LoginController(IAuthService authService)
        {
            _authService = authService;
        }

        [Authorize]
        [HttpPut]
        public IActionResult Put() { return Ok("hola entro"); }

        [HttpPost]
        public IActionResult Post([FromBody] LoginDTO loginDto)
        {
            if (loginDto is null) return BadRequest();
            var result = _authService.Login(loginDto);
            if (result.Token is not null)
                return Ok(result.Token);
            return BadRequest(result);
        }

        [HttpGet]
        public IActionResult Getdata()
        {
            var stream = HttpContext.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(stream)) return Unauthorized();

            var data = _authService.GetUserData(stream);
            if (data is null) return Unauthorized();
            return Ok(data);
        }
    }
}
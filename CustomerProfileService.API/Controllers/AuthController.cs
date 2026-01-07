using CustomerProfileService.API.Helpers;
using CustomerProfileService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CustomerProfileService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    [AllowAnonymous]
    [HttpPost("registerUser")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        try
        {
            if (user == null)
                return BadRequest("Invalid request.");
            await _authService.RegisterAsync(user);

            return Ok(new
            {
                message = "User registered successfully"
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message); // 400
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message); // 500
        }
    }


    // 🔹 LOGIN
    [AllowAnonymous]
    [HttpPost("loginUser")]
    public async Task<IActionResult> Login([FromBody] User request)
    {
        try
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var user = await _authService.LoginAsync(request.Email, request.Password);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            var token = JwtTokenHelper.GenerateToken(user, _config);

            return Ok(new { token });
        }
        catch (ValidationException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

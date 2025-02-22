using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Requests;
using SharedLibrary.Responses;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthenticationController(IAuthService authService) 
    { 
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user and logs them in if successful.
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register(AuthenticationRequest request)
    {
        var (success, content) = _authService.Register(request.Username, request.Password);
        if (!success) return BadRequest(content); // Username already exists

        return Login(request); // Send new user directly to login
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login(AuthenticationRequest request)
    {
        var (success, content) = _authService.Login(request.Username, request.Password);
        if (!success) return BadRequest(content); // Login Credentials are invalid

        return Ok(new AuthenticationResponse() { Token = content });
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("Authenication Server is OK");
    }
}

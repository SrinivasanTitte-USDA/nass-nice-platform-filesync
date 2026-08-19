using Microsoft.AspNetCore.Mvc;

namespace NICE.Platform.FileSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TokenController : ControllerBase
{
    public TokenController()
    {

    }
    [HttpGet]
    public async Task<IActionResult> GetToken()
    {
        // Generate a token (this is just a placeholder, implement your own logic)
        var token = Guid.NewGuid().ToString();
        return Ok(new { Token = token });
    }
}

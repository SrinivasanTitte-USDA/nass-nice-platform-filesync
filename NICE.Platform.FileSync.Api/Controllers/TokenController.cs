using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NICE.Platform.FileSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TokenController(ServerCertificateService certService, ClientRegistry registry, TokenRequest req) : ControllerBase
{
    private readonly ServerCertificateService _certService = certService;
    private readonly ClientRegistry _registry = registry;
    private readonly TokenRequest _req = req;

    //[HttpPost]
    [HttpGet]
    public async Task<IActionResult> GetToken()
    {
        // Generate a token (this is just a placeholder, implement your own logic)
        //var token = Guid.NewGuid().ToString();
        //return Ok(new { Token = token });
        if (!_registry.IsValidClient(_req.ClientId))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid client: {_req.ClientId}");
            return Unauthorized();
        }
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "nice.nass.usda.gov",
            Subject = new ClaimsIdentity(new[] { new Claim("client_id", _req.ClientId) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new X509SecurityKey(_certService.Certificate), SecurityAlgorithms.RsaSha256)
        };

        var token = handler.CreateToken(descriptor);
        System.Diagnostics.Debug.WriteLine($"Token request for client: {req.ClientId}");

        return Ok(new { token = handler.WriteToken(token) });
    }

}

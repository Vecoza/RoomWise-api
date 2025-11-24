
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/auth-debug")]
public class AuthDebugController : ControllerBase
{
    [HttpPost("parse-token")]
    public IActionResult ParseToken([FromBody] string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            return Ok(new
            {
                Header = jwt.Header,
                Payload = jwt.Payload
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                ex.Message,
                ex.StackTrace
            });
        }
    }
}

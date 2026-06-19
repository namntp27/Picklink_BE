using Microsoft.AspNetCore.Mvc;
using Picklink.Application.Common;

namespace Picklink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            service = "Picklink API",
            status = "Healthy",
            time = DateTimeOffset.UtcNow
        }));
    }
}

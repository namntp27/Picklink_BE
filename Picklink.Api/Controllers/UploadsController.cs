using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Picklink.Application.Common;
using Picklink.Application.Interfaces;

namespace Picklink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UploadsController(IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost("images")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new AppException("File is empty.", 400);
        }

        await using var stream = file.OpenReadStream();
        var url = await fileStorageService.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { url }, "File uploaded."));
    }
}

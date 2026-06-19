using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picklink.Application.Common;
using Picklink.Application.DTOs;
using Picklink.Infrastructure.Data;

namespace Picklink.Api.Controllers;

[ApiController]
[Route("api/administrative-units")]
public sealed class AdministrativeUnitsController(PicklinkDbContext dbContext) : ControllerBase
{
    [HttpGet("provinces")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProvinceResponse>>>> GetProvinces(CancellationToken cancellationToken)
    {
        var items = await dbContext.AdministrativeProvinces.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProvinceResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ProvinceResponse>>.Ok(items));
    }

    [HttpGet("provinces/{provinceId:guid}/wards")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WardResponse>>>> GetWards(Guid provinceId, CancellationToken cancellationToken)
    {
        var items = await dbContext.AdministrativeWards.AsNoTracking()
            .Where(x => x.ProvinceId == provinceId)
            .OrderBy(x => x.Name)
            .Select(x => new WardResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WardResponse>>.Ok(items));
    }
}

using HospitalManagement.Api.Authorization;
using HospitalManagement.Api.DTOs;
using HospitalManagement.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/laboratory")]
public class LaboratoryController(ILaboratoryService laboratoryService) : ControllerBase
{
    [HttpGet]
    [HasPermission("laboratory.read")]
    [ProducesResponseType(typeof(ApiResponse<LaboratoryPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPage(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(ApiResponse.Failure("Page must be positive and pageSize must be between 1 and 100."));

        var result = await laboratoryService.GetPageAsync(search, page, pageSize, cancellationToken);
        return Ok(ApiResponse<LaboratoryPageDto>.Ok(result));
    }

    [HttpGet("lookup")]
    [HasPermission("laboratory.read")]
    [ProducesResponseType(typeof(ApiResponse<LaboratoryLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLookup(CancellationToken cancellationToken)
    {
        var lookup = await laboratoryService.GetLookupAsync(cancellationToken);
        return Ok(ApiResponse<LaboratoryLookupDto>.Ok(lookup));
    }

    [HttpPost]
    [HasPermission("laboratory.manage")]
    [ProducesResponseType(typeof(ApiResponse<LabOrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateLabOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await laboratoryService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<LabOrderDetailDto>.Ok(result, "Lab order created."));
    }
}

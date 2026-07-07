using CloudGuard.Api.DTOs.Terraform;
using CloudGuard.Api.Services.Terraform;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/terraform")]
public class TerraformController(ITerraformUploadService uploadService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(TerraformUploadDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TerraformUploadDetailDto>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest(new { message = "File is missing. Use form field 'file'." });

        try
        {
            var result = await uploadService.UploadAsync(file, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("uploads")]
    [ProducesResponseType(typeof(IReadOnlyList<TerraformUploadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TerraformUploadDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var uploads = await uploadService.GetAllAsync(cancellationToken);
        return Ok(uploads);
    }

    [HttpGet("uploads/{id:int}")]
    [ProducesResponseType(typeof(TerraformUploadDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TerraformUploadDetailDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var upload = await uploadService.GetByIdAsync(id, cancellationToken);

        if (upload is null)
            return NotFound(new { message = $"Upload with id {id} was not found." });

        return Ok(upload);
    }
}

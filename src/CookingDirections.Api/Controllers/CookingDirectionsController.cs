using CookingDirections.Application.Contracts;
using CookingDirections.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookingDirections.Api.Controllers;

[ApiController]
[Route("api/cooking-directions")]
public sealed class CookingDirectionsController(ICookingDirectionService cookingDirectionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CookingDirectionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var cookingDirections = await cookingDirectionService.GetAllAsync(cancellationToken);
        return Ok(cookingDirections);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CookingDirectionDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var cookingDirection = await cookingDirectionService.GetByIdAsync(id, cancellationToken);
        return cookingDirection is null ? NotFound() : Ok(cookingDirection);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CookingDirectionDto>> Create(CookingDirectionRequest request, CancellationToken cancellationToken)
    {
        var cookingDirection = await cookingDirectionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = cookingDirection.Id }, cookingDirection);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CookingDirectionDto>> Update(int id, CookingDirectionRequest request, CancellationToken cancellationToken)
    {
        var cookingDirection = await cookingDirectionService.UpdateAsync(id, request, cancellationToken);
        return cookingDirection is null ? NotFound() : Ok(cookingDirection);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await cookingDirectionService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

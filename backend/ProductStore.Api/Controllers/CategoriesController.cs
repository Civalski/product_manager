using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductStore.Api.DTOs;
using ProductStore.Api.Services;

namespace ProductStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CategoriesController(
    ICategoryService categoryService,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<CreateCategoryFieldRequest> createFieldValidator,
    ILogger<CategoriesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(CancellationToken cancellationToken)
    {
        var list = await categoryService.ListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var c = await categoryService.GetByIdAsync(id, cancellationToken);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        logger.LogInformation("Criando categoria Name={Name}", request.Name);
        var created = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/fields")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryFieldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoryFieldResponse>>> ListFields(Guid id, CancellationToken cancellationToken)
    {
        var list = await categoryService.ListFieldsAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/fields")]
    [ProducesResponseType(typeof(CategoryFieldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryFieldResponse>> CreateField(
        Guid id,
        [FromBody] CreateCategoryFieldRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createFieldValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var created = await categoryService.AddFieldAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(ListFields), new { id }, created);
    }

    [HttpDelete("{id:guid}/fields/{fieldId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteField(Guid id, Guid fieldId, CancellationToken cancellationToken)
    {
        await categoryService.DeleteFieldAsync(id, fieldId, cancellationToken);
        return NoContent();
    }
}

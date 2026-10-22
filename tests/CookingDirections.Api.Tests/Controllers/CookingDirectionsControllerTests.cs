using CookingDirections.Api.Controllers;
using CookingDirections.Application.Contracts;
using CookingDirections.Application.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CookingDirections.Api.Tests.Controllers;

public sealed class CookingDirectionsControllerTests
{
    private readonly ICookingDirectionService _cookingDirectionService = Substitute.For<ICookingDirectionService>();
    private readonly CookingDirectionsController _controller;

    public CookingDirectionsControllerTests()
    {
        _controller = new CookingDirectionsController(_cookingDirectionService);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCookingDirections()
    {
        IReadOnlyList<CookingDirectionDto> cookingDirections = [CreateDto(1), CreateDto(2)];
        _cookingDirectionService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(cookingDirections);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(cookingDirections, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenCookingDirectionExists_ReturnsOk()
    {
        var cookingDirection = CreateDto(3);
        _cookingDirectionService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(cookingDirection);

        var result = await _controller.GetById(3, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(cookingDirection, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenCookingDirectionDoesNotExist_ReturnsNotFound()
    {
        _cookingDirectionService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((CookingDirectionDto?)null);

        var result = await _controller.GetById(3, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGetById()
    {
        var request = CreateRequest();
        var created = CreateDto(12);
        _cookingDirectionService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CookingDirectionsController.GetById), createdResult.ActionName);
        Assert.Equal(12, createdResult.RouteValues?["id"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Update_WhenCookingDirectionExists_ReturnsOk()
    {
        var request = CreateRequest();
        var updated = CreateDto(4);
        _cookingDirectionService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _controller.Update(4, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenCookingDirectionDoesNotExist_ReturnsNotFound()
    {
        var request = CreateRequest();
        _cookingDirectionService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns((CookingDirectionDto?)null);

        var result = await _controller.Update(4, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(true, typeof(NoContentResult))]
    [InlineData(false, typeof(NotFoundResult))]
    public async Task Delete_ReturnsExpectedResult(bool deleted, Type expectedResultType)
    {
        _cookingDirectionService.DeleteAsync(6, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _controller.Delete(6, CancellationToken.None);

        Assert.IsType(expectedResultType, result);
    }

    private static CookingDirectionRequest CreateRequest() => new(11, 1, "Preheat the oven.", 10, 425);

    private static CookingDirectionDto CreateDto(int id) => new(
        id,
        11,
        1,
        "Preheat the oven.",
        10,
        425,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch);
}

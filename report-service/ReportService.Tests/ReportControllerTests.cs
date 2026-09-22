using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using report_service.Controllers;
using report_service.DTO;
using report_service.Services;
using Xunit;

namespace ReportService.Tests;

public class ReportControllerTests
{
    private readonly Mock<IFinanceServiceClient> _financeServiceClientMock = new();
    private readonly Mock<IReportEngine> _reportEngineMock = new();
    private readonly ReportController _controller;

    public ReportControllerTests()
    {
        _controller = new ReportController(_financeServiceClientMock.Object, _reportEngineMock.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsBadRequest_WhenStartDateIsAfterEndDate()
    {
        var startDate = DateTime.Now;
        var endDate = startDate.AddDays(-1);

        var result = await _controller.GetSummary(startDate, endDate);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ReturnsUnauthorized_WhenUserIdIsInvalid()
    {
        _controller.ControllerContext = TestHelper.CreateControllerContextWithInvalidUserId();

        var result = await _controller.GetSummary(DateTime.Now.AddDays(-7), DateTime.Now);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        _controller.ControllerContext = TestHelper.CreateControllerContextWithoutUserId();

        var result = await _controller.GetSummary(DateTime.Now.AddDays(-7), DateTime.Now);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Theory]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData("sub")]
    [InlineData("userId")]
    [InlineData("id")]
    public async Task GetSummary_ResolvesUserIdFromSupportedClaims(string claimType)
    {
        const int userId = 7;
        var startDate = new DateTime(2026, 5, 1);
        var endDate = new DateTime(2026, 5, 31);
        var transactions = new List<TransactionDto>();
        var reportSummary = new ReportSummaryDto { ItemsByCategory = [] };

        _financeServiceClientMock
            .Setup(client => client.GetTransactionsForReportAsync(userId, startDate, endDate))
            .ReturnsAsync(transactions);

        _reportEngineMock
            .Setup(engine => engine.GenerateSummary(startDate, endDate, transactions))
            .Returns(reportSummary);

        _controller.ControllerContext = TestHelper.CreateControllerContextWithUserId(userId, claimType);

        var result = await _controller.GetSummary(startDate, endDate);

        Assert.IsType<OkObjectResult>(result);
        _financeServiceClientMock.Verify(client => client.GetTransactionsForReportAsync(userId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetSummary_ReturnsOk_WithReportSummary()
    {
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;
        const int userId = 1;

        var transactions = new List<TransactionDto>
        {
            new() { Amount = 100, Type = "INCOME", CategoryName = "Salary" },
            new() { Amount = 50, Type = "EXPENSE", CategoryName = "Food" }
        };

        var reportSummary = new ReportSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalIncome = 100,
            TotalExpense = 50,
            Balance = 50,
            ItemsByCategory = []
        };

        _financeServiceClientMock
            .Setup(client => client.GetTransactionsForReportAsync(userId, startDate, endDate))
            .ReturnsAsync(transactions);

        _reportEngineMock
            .Setup(engine => engine.GenerateSummary(startDate, endDate, transactions))
            .Returns(reportSummary);

        _controller.ControllerContext = TestHelper.CreateControllerContextWithUserId(userId);

        var result = await _controller.GetSummary(startDate, endDate);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(reportSummary, okResult.Value);
    }

    [Fact]
    public async Task GetSummary_ReturnsInternalServerError_WhenFinanceClientThrows()
    {
        const int userId = 1;
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        _financeServiceClientMock
            .Setup(client => client.GetTransactionsForReportAsync(userId, startDate, endDate))
            .ThrowsAsync(new HttpRequestException("Finance service unavailable"));

        _controller.ControllerContext = TestHelper.CreateControllerContextWithUserId(userId);

        var result = await _controller.GetSummary(startDate, endDate);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}

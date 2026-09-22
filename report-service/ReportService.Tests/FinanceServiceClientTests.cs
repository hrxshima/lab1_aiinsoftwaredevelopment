using System.Net;
using System.Net.Http.Json;
using System.Text;
using report_service.DTO;
using report_service.Services;
using Xunit;

namespace ReportService.Tests;

public class FinanceServiceClientTests
{
    [Fact]
    public async Task GetTransactionsForReportAsync_ReturnsTransactions_OnSuccess()
    {
        var expected = new List<TransactionDto>
        {
            new() { Amount = 100, Type = "INCOME", CategoryName = "Salary" }
        };

        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            }));

        var client = CreateClient(handler);
        var service = new FinanceServiceClient(client);

        var result = await service.GetTransactionsForReportAsync(1, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Single(result);
        Assert.Equal(100, result[0].Amount);
        Assert.Equal("/internal/transactions/report?userId=1&startDate=2026-05-01&endDate=2026-05-31", handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetTransactionsForReportAsync_ReturnsEmptyList_WhenResponseBodyIsNull()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler);
        var service = new FinanceServiceClient(client);

        var result = await service.GetTransactionsForReportAsync(2, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTransactionsForReportAsync_ThrowsHttpRequestException_OnFailure()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid range", Encoding.UTF8, "text/plain")
            }));

        var client = CreateClient(handler);
        var service = new FinanceServiceClient(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetTransactionsForReportAsync(1, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));

        Assert.Contains("BadRequest", exception.Message);
        Assert.Contains("invalid range", exception.Message);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri("http://transaction-service:8080") };
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return handler(request);
        }
    }
}

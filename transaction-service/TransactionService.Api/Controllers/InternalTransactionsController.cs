using Microsoft.AspNetCore.Mvc;
using TransactionService.Api.Dtos;
using TransactionService.Api.Services;

namespace TransactionService.Api.Controllers;

[ApiController]
[Route("internal/transactions")]
public class InternalTransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public InternalTransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("report")]
    public async Task<ActionResult<List<TransactionReportResponse>>> GetReport(
        [FromQuery] int userId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            return await _transactionService.GetReportAsync(userId, startDate, endDate);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}

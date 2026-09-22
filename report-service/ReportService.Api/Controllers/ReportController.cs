using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using report_service.Services;

namespace report_service.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly IFinanceServiceClient _financeServiceClient;
        private readonly IReportEngine _reportEngine;

        public ReportController(IFinanceServiceClient financeServiceClient, IReportEngine reportEngine)
        {
            _financeServiceClient = financeServiceClient;
            _reportEngine = reportEngine;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    error = "Bad Request",
                    message = "Start date cannot be after end date"
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("userId")?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    error = "Unauthorized",
                    message = "User ID not found in authorization token"
                });
            }

            try
            {
                var transactions = await _financeServiceClient.GetTransactionsForReportAsync(userId, startDate, endDate);

                var reportSummary = _reportEngine.GenerateSummary(startDate, endDate, transactions);

                return Ok(reportSummary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    error = "Internal Server Error",
                    message = ex.Message
                });
            }
        }
    }
}

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Prometheus;
using SharedLibrary;

namespace SearchAPI.Controller;

[ApiController]
[Route("api/search")]
public class SearchAPIController : ControllerBase
{
    private readonly DbContextConfig _dbContext;
    private readonly ILogger<SearchAPIController> _logger;
    private readonly IAsyncPolicy _databaseResiliencePolicy;
    
    private static readonly Counter TotalSearchRequests = Metrics.CreateCounter(
        "search_requests_total", "Total search requests received");

    private static readonly Counter TotalSearchErrors = Metrics.CreateCounter(
        "search_errors_total", "Total failed search requests");

    private static readonly Histogram SearchResponseTime = Metrics.CreateHistogram(
        "search_response_time_seconds", "Histogram of search response times");


    public SearchAPIController(DbContextConfig dbContext, ILogger<SearchAPIController> logger, IAsyncPolicy databaseResiliencePolicy)
    {
        _dbContext = dbContext;
        _logger = logger;
        _databaseResiliencePolicy = databaseResiliencePolicy;
    }

    [HttpGet("query")]
    public async Task<IActionResult> SearchEmail([FromQuery] string query)
    {
        using (SearchResponseTime.NewTimer())
        {
            _logger.LogInformation("Received search request with query: {Query}", query);
            TotalSearchRequests.Inc();

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty search query received.");
                return BadRequest(new { message = "Query cannot be empty." });
            }

            try
            {
                var results = await _databaseResiliencePolicy.ExecuteAsync(async () =>
                {
                    return await _dbContext.Files
                        .Select(f => new
                        {
                            f.FileId,
                            f.FileName,
                            Content = Encoding.UTF8.GetString(f.Content)
                        })
                        .ToListAsync();

                }).ConfigureAwait(false);
                
                
                if (!results.Any())
                {
                    _logger.LogInformation("No results found for query: {Query}", query);
                    return NotFound(new { message = "No matching emails found." });
                }
                var filteredResults = results.Where(f => f.Content.Contains(query)).ToList();



                _logger.LogInformation("Found {ResultCount} matching results for query: {Query}", results.Count, query);
                return Ok(filteredResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for query: {Query}", query);
                TotalSearchErrors.Inc();
                return StatusCode(500, new { message = "An error occurred while processing your request." + ex.Message  });
            }
        }
    }
}
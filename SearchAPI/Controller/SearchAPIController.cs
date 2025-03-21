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


    public SearchAPIController(DbContextConfig dbContext, ILogger<SearchAPIController> logger,
        IAsyncPolicy databaseResiliencePolicy)
    {
        _dbContext = dbContext;
        _logger = logger;
        _databaseResiliencePolicy = databaseResiliencePolicy;
    }
    
   [HttpGet("query")]
public async Task<IActionResult> SearchWord([FromQuery] string query)
{
    using (SearchResponseTime.NewTimer())
    {
        _logger.LogInformation("Received word search request: {Query}", query);
        TotalSearchRequests.Inc();

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty search query received.");
            return BadRequest(new { message = "Query cannot be empty." });
        }

        try
        {
            string lowerQuery = query.ToLower(); 

            var results = await _databaseResiliencePolicy.ExecuteAsync(async () =>
            {
                return await _dbContext.Words
                    .Where(w => EF.Functions.Like(w.WordText.ToLower(), $"%{lowerQuery}%")) 
                    .Select(w => new
                    {
                        w.WordId,
                        w.WordText,
                        OccurrenceCount = w.Occurrences.Count(),
                        Files = w.Occurrences.Select(o => new
                        {
                            o.File.FileId,
                            o.File.FileName
                        }).Distinct().ToList() 
                    })
                    .OrderByDescending(w => w.OccurrenceCount)
                    .ToListAsync();
            }).ConfigureAwait(false);

            if (results.Count == 0)
            {
                _logger.LogInformation("No matching words found for query: {Query}", query);
                return NotFound(new { message = "No matching words found." });
            }

            _logger.LogInformation("Found {ResultCount} matching words for query: {Query}", results.Count, query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for query: {Query}", query);
            TotalSearchErrors.Inc();
            return StatusCode(500, new { message = "An error occurred while processing your request: " + ex.Message });
        }
    }
}

[HttpGet("files/download/{fileId}")]
public async Task<IActionResult> DownloadFile(int fileId)
{
    var file = await _dbContext.Files.FindAsync(fileId);

    if (file == null)
    {
        return NotFound(new { message = "File not found." });
    }
    
    var content = Encoding.UTF8.GetString(file.Content);
    var byteArray = Encoding.UTF8.GetBytes(content);
    var fileStream = new MemoryStream(byteArray);
    
    return File(fileStream, "text/plain", $"{file.FileName}");
}



}
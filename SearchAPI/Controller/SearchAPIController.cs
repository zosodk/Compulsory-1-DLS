using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedLibrary;

namespace SearchAPI.Controller;

public class SearchAPIController : ControllerBase
{
   private readonly DbContextConfig _dbContext;

   public SearchAPIController(DbContextConfig dbContext)
   {
      _dbContext = dbContext;
   }


   [HttpGet("query")]
   public async Task<IActionResult> SearchEmail(string query)
   {
      var results = await _dbContext.Files
         .Where(f => Encoding.UTF8.GetString(f.Content).Contains(query))
         .Select(f => new
         {
            f.FileId,
            f.FileName,
            Content = Encoding.UTF8.GetString(f.Content)
         })
         .ToListAsync();
      
      if (!results.Any())
         return NotFound(new { message = "No matching emails found." });
     
      return Ok(results);
   }
}
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
 
namespace BlobArchiveFunctions;
 
public class ManualArchive
{
    private readonly BlobArchiveService _archiveService;
    private readonly ILogger<ManualArchive> _logger;
 
    public ManualArchive(BlobArchiveService archiveService, ILogger<ManualArchive> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }
 
    [Function("ManualArchive")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        // Optional: pass ?days=1 to archive files older than 1 day (useful for testing)
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        int days = int.TryParse(query["days"], out var d) ? d : 7;
 
        _logger.LogInformation("ManualArchive triggered — archiving files older than {Days} day(s).", days);
 
        int count = await _archiveService.ArchiveOldBlobsAsync(olderThanDays: days);
 
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Archive complete. Files moved: {count}");
        return response;
    }
}

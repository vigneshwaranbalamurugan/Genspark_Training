using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
 
namespace BlobArchiveFunctions;
 
public class NightlyArchive
{
    private readonly BlobArchiveService _archiveService;
    private readonly ILogger<NightlyArchive> _logger;
 
    public NightlyArchive(BlobArchiveService archiveService, ILogger<NightlyArchive> logger)
    {
        _archiveService = archiveService;
        _logger = logger;
    }
 
    // Runs every day at midnight UTC
    // Cron format: {second} {minute} {hour} {day} {month} {day-of-week}
    [Function("NightlyArchive")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("NightlyArchive triggered at: {Time}", DateTime.UtcNow);
        await _archiveService.ArchiveOldBlobsAsync(olderThanDays: 7);
    }
}

using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nckr.Demo.SimpleEventApi.Setup;

namespace Nckr.Demo.SimpleEventApi.Setup;

public class InitializeTables
{
    private readonly ILogger<InitializeTables> _logger;
    private readonly ITableClientFactory _tableClientFactory;

    public InitializeTables(ILogger<InitializeTables> logger, ITableClientFactory tableClientFactory)
    {
        _logger = logger;
        _tableClientFactory = tableClientFactory;
    }

    [Function("InitializeTables")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("InitializeTables function triggered.");

        try
        {
            // Use the factory to ensure the tables exist
            await _tableClientFactory.EnsureTablesExistAsync();

            return new OkObjectResult(new
            {
                message = "Table initialization complete."
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error checking/creating tables: {Message}", ex.Message);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during table initialization: {Message}", ex.Message);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

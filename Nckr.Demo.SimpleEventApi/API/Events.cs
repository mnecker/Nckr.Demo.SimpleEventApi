using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;

namespace Nckr.Demo.SimpleEventApi.API;

public class Events
{
    private readonly ILogger<Events> _logger;
	private readonly ITableClientFactory _tableClientFactory;

	public Events(ILogger<Events> logger, ITableClientFactory tableClientFactory)
    {
        _logger = logger;
        _tableClientFactory = tableClientFactory;
    }

    [Function("GetEvents")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events")] HttpRequest req)
    {
		string subscriptionKey = "";
		if (req.Headers.TryGetValue("Ocp-Apim-Subscription-Key", out StringValues headerValues))
		{
			subscriptionKey = headerValues.FirstOrDefault();
		}
		else
		{
			return new UnauthorizedResult();
		}
		
		TableClient eventClient = _tableClientFactory.GetEventService();
		var events = eventClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}'");


		return new JsonResult(events);
    }
}
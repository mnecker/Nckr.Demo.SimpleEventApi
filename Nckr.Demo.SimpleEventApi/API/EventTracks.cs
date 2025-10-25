using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;

namespace Nckr.Demo.SimpleEventApi.API;

public class EventTracks
{
	private readonly ILogger<EventTracks> _logger;
	private readonly ITableClientFactory _tableClientFactory;

	public EventTracks(ILogger<EventTracks> logger, ITableClientFactory tableClientFactory)
	{
		_logger = logger;
		_tableClientFactory = tableClientFactory;
	}

	[Function("GetEventTracks")]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/tracks")] HttpRequest req,
		string eventId)
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

		TableClient trackClient = _tableClientFactory.GetEventTrackService();
		var tracks = trackClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}'");

		return new JsonResult(tracks);
	}
}
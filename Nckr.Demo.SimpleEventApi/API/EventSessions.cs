using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;

namespace Nckr.Demo.SimpleEventApi.API;

public class EventSessions
{
	private readonly ILogger<EventSessions> _logger;
	private readonly ITableClientFactory _tableClientFactory;

	public EventSessions(ILogger<EventSessions> logger, ITableClientFactory tableClientFactory)
	{
		_logger = logger;
		_tableClientFactory = tableClientFactory;
	}

	[Function("GetEventSessions")]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/sessions")] HttpRequest req,
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

		TableClient sessionClient = _tableClientFactory.GetEventSessionService();
		var sessions = sessionClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}'");

		return new JsonResult(sessions);
	}

	[Function("GetEventSessionsByTrack")]
	public async Task<IActionResult> RunByTrack([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/tracks/{trackid}/sessions")] HttpRequest req,
		string eventId,
		string trackId)
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

		TableClient sessionClient = _tableClientFactory.GetEventSessionService();
		var sessions = sessionClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}' and TrackId eq '{trackId}'");

		return new JsonResult(sessions);
	}
}

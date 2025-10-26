using Azure.Data.Tables;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;
using System;
using System.IO;
using System.Text.Json;

namespace Nckr.Demo.SimpleEventApi.API;

public class Webhooks
{
    private readonly ILogger<Webhooks> _logger;
    private readonly ITableClientFactory _tableClientFactory;

    public Webhooks(ILogger<Webhooks> logger, ITableClientFactory tableClientFactory)
    {
        _logger = logger;
        _tableClientFactory = tableClientFactory;
    }

    [Function("GetWebhooks")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/webhooks")] HttpRequest req,
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

        TableClient webhookClient = _tableClientFactory.GetWebhookService();
        var webhooks = webhookClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}'");

        return new JsonResult(webhooks);
    }

    [Function("PostWebhook")]
    public async Task<IActionResult> RunRegister([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{eventid}/webhooks")] HttpRequest req,
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

        string body;
        using (var reader = new StreamReader(req.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        // Parse the incoming JSON into a Webhook object
        Webhook? webhook;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            webhook = JsonSerializer.Deserialize<Webhook>(body, options);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Invalid JSON payload");
        }

        if (webhook == null)
        {
            return new BadRequestObjectResult("Webhook payload could not be parsed");
        }

        // Ensure partition/row keys and event id are set
        webhook.PartitionKey = $"{subscriptionKey}-{eventId}";
        webhook.RowKey = Guid.NewGuid().ToString();
        webhook.EventId = eventId;

        TableClient webhookClient = _tableClientFactory.GetWebhookService();
        await webhookClient.AddEntityAsync<Webhook>(webhook);

        string baseUrl = "http://localhost:7079/api";
        string deleteUrl = $"/events/{eventId}/webhooks/{webhook.RowKey}";
        CreatedResult response = new CreatedResult(deleteUrl, webhook);

        return response;
    }

	[Function("DeleteWebhook")]
	public async Task<IActionResult> RunDelete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/{eventid}/webhooks/{webhookid}")] HttpRequest req,
		string eventId,
        string webhookId)
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

		string partitionKey = $"{subscriptionKey}-{eventId}";
		TableClient webhookClient = _tableClientFactory.GetWebhookService();
        var response = await webhookClient.DeleteEntityAsync(partitionKey, webhookId);

		return new JsonResult(response);
	}
}
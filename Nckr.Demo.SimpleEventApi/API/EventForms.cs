using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nckr.Demo.SimpleEventApi.API;

public class EventForms
{
	private readonly ILogger<EventForms> _logger;
	private readonly ITableClientFactory _tableClientFactory;

	public EventForms(ILogger<EventForms> logger, ITableClientFactory tableClientFactory)
	{
		_logger = logger;
		_tableClientFactory = tableClientFactory;
	}

	[Function("GetEventForms")]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/forms")] HttpRequest req,
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

		TableClient formClient = _tableClientFactory.GetEventFormService();
		var forms = formClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}'");

		return new JsonResult(forms);
	}

	[Function("GetEventFormSchema")]
	public async Task<IActionResult> RunGetSchema([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{eventid}/forms/{formid}")] HttpRequest req,
	string eventId,
	string formId)
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

		TableClient formClient = _tableClientFactory.GetEventFormService();
		var forms = formClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{subscriptionKey}-{eventId}' and RowKey eq '{formId}'");
		string schema = "";

		await foreach (Page<TableEntity> page in forms.AsPages())
		{
			foreach (TableEntity entity in page.Values)
			{
				// Map TableEntity to EventForm
				var form = new EventForm
				{
					PartitionKey = entity.PartitionKey,
					RowKey = entity.RowKey,
					Timestamp = entity.Timestamp,
					ETag = entity.ETag,
					EventId = entity.GetString(nameof(EventForm.EventId)),
					Name = entity.GetString(nameof(EventForm.Name)),
					Description = entity.GetString(nameof(EventForm.Description)),
					Format = entity.GetString(nameof(EventForm.Format))
				};
				schema = form.Format;
			}
		}
		var jsonSchema = JsonObject.Parse(schema);
		return new ObjectResult(jsonSchema);
	}

	[Function("SubmitEventForm")]
	public async Task<IActionResult> RunSubmitForm([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{eventid}/forms/{formid}")] HttpRequest req,
	string eventId,
	string formId)
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

		// Read the request body as a string
		string body;
		using (var reader = new StreamReader(req.Body))
		{
			body = await reader.ReadToEndAsync();
		}

		TableClient formSubmissionClient = _tableClientFactory.GetEventFormSubmissionService();
		EventFormSubmission submission = new EventFormSubmission
		{
			PartitionKey = $"{subscriptionKey}-{eventId}-{formId}",
			RowKey = Guid.NewGuid().ToString(),
			FormId = formId,
			ResponseData = body
		};
		await formSubmissionClient.AddEntityAsync<EventFormSubmission>(submission);

		ExecuteWebhooks(submission, eventId, subscriptionKey);

		return new JsonResult(submission);
	}

	private async Task ExecuteWebhooks(EventFormSubmission submission, string eventId, string subcriptionKey)
	{
		_logger.LogInformation("Executing webhooks for form submission {SubmissionId}", submission.RowKey);

		string partitionKey = $"{subcriptionKey}-{eventId}";
		_logger.LogInformation("Looking for webhooks with partition key {PartitionKey}", partitionKey);
		TableClient webhooks = _tableClientFactory.GetWebhookService();
		var webhookEntities = webhooks.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'");
		await foreach (Page<TableEntity> page in webhookEntities.AsPages())
		{
			foreach (TableEntity entity in page.Values)
			{
				string url = entity.GetString("CallBackUrl");
				string method = "POST";
				_logger.LogInformation($"Webhook URL: {url}");
				using (var httpClient = new HttpClient())
				{
					EventNotification webhookPayload = new EventNotification
					{
						EventId = eventId,
						Topic = "Forms",
						Content = JsonObject.Parse(submission.ResponseData).ToJsonString()
					};
					var content = new StringContent(JsonSerializer.Serialize(webhookPayload), System.Text.Encoding.UTF8, "application/json");
					HttpResponseMessage response = method.ToUpper() switch
					{
						"POST" => await httpClient.PostAsync(url, content),
						"PUT" => await httpClient.PutAsync(url, content),
						_ => throw new NotSupportedException($"HTTP method {method} is not supported for webhooks.")
					};
					if (response.IsSuccessStatusCode)
					{
						_logger.LogInformation("Successfully executed webhook {WebhookUrl} for submission {SubmissionId}", url, submission.RowKey);
					}
					else
					{
						_logger.LogWarning("Failed to execute webhook {WebhookUrl} for submission {SubmissionId}. Status Code: {StatusCode}", url, submission.RowKey, response.StatusCode);
					}
				}
			}
		}
	}
}
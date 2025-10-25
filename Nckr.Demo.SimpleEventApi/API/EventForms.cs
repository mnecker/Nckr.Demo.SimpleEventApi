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
		return new ObjectResult(schema);
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

		return new JsonResult(submission);
	}
}
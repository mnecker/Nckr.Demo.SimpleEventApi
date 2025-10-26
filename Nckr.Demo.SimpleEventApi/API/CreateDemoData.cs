using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nckr.Demo.SimpleEventApi.Models;
using Nckr.Demo.SimpleEventApi.Setup;
using System.Threading.Tasks;

namespace Nckr.Demo.SimpleEventApi.API;

public class CreateDemoData
{
    private readonly ILogger<CreateDemoData> _logger;
    private readonly ITableClientFactory _tableClientFactory;

    public CreateDemoData(ILogger<CreateDemoData> logger, ITableClientFactory tableClientFactory)
    {
        _logger = logger;
        _tableClientFactory = tableClientFactory;
    }

    [Function("CreateDemoData")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "setup/createdemodata")] HttpRequest req)
    {
        string subscriptionKey = "";
        if (req.Headers.TryGetValue("Ocp-Apim-Subscription-Key", out StringValues headerValues))
        {
           subscriptionKey = headerValues.FirstOrDefault();
		} else
        {
			return new UnauthorizedResult();
		}

        try
        {
            // Ensure tables exist
            await _tableClientFactory.EnsureTablesExistAsync();

            CreateEventData(_tableClientFactory.GetEventService(), subscriptionKey);
			CreateEventTrackData(_tableClientFactory.GetEventTrackService(), subscriptionKey);
			CreateEventSessionData(_tableClientFactory.GetEventSessionService(), subscriptionKey);
			CreateEventFormData(_tableClientFactory.GetEventFormService(), subscriptionKey);

			return new OkObjectResult(new { message = "Table initialization complete." });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error working with Azure Tables: {Message}", ex.Message);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {Message}", ex.Message);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private void CreateEventData(TableClient tableClient, string tenantId) 
    {
        foreach (EventEntity e in GetEvents(tenantId))
        {
            tableClient.UpsertEntity(e);
		}
	}

	private void CreateEventTrackData(TableClient tableClient, string tenantId)
	{
		foreach (EventEntity e in GetEvents(tenantId))
		{
			foreach (EventTrack t in GetEventTracks(tenantId, e.RowKey))
			{
				tableClient.UpsertEntity(t);
			}
		}
	}

	private void CreateEventSessionData(TableClient tableClient, string tenantId)
	{
		foreach (EventEntity e in GetEvents(tenantId))
		{
			foreach (EventSession s in GetEventSessions(tenantId, e.RowKey))
			{
				tableClient.UpsertEntity(s);
			}	
		}
	}

	private void CreateEventFormData(TableClient tableClient, string tenantId)
	{
		foreach (EventEntity e in GetEvents(tenantId))
		{
			foreach (EventForm f in GetEventForms(tenantId, e.RowKey))
			{
				tableClient.UpsertEntity(f);
			}
		}
	}


	private List<EventEntity> GetEvents(string tenantId)
    {
		List<EventEntity> events = new List<EventEntity>();
		events.Add(new EventEntity
		{
			PartitionKey = tenantId,
			RowKey = "0D8F2A1D-5398-4B63-BA11-32F12021D10A",
			Name = "Nordic Summit 2025",
			Description = "The Power Platform confernce in the Nordics!",
			StartDate = new DateTime(2025, 9, 20, 1, 0, 0, 0, DateTimeKind.Utc),
			EndDate = new DateTime(2025, 9, 21, 1, 0, 0, 0, DateTimeKind.Utc),
			WebsiteUrl = "https://nordicsummit.info/",
			Country = "Gothenburg, Schweden"
		});
		events.Add(new EventEntity
		{
			PartitionKey = tenantId,
			RowKey = "D406AC81-AF37-48E2-AED0-1147FFCDFF23",
			Name = "South Coast Summit 2025",
			Description = "Microsoft Technology in the south of UK",
			StartDate = new DateTime(2025, 10, 17, 1, 0, 0, 0, DateTimeKind.Utc),
			EndDate = new DateTime(2025, 10, 18, 1, 0, 0, 0, DateTimeKind.Utc),
			WebsiteUrl = "https://www.southcoastsummit.com/",
			Country = "Farnborough, UK"
		});
		events.Add(new EventEntity
		{
			PartitionKey = tenantId,
			RowKey = "54434FB9-66EC-4D93-B281-18EE129110AF",
			Name = "Power Platform Community Conference 2025",
			Description = "The biggest Power Platform Conference in the world!",
			StartDate = new DateTime(2025, 10, 26, 1, 0, 0, 0, DateTimeKind.Utc),
			EndDate = new DateTime(2025, 10, 31, 1, 0, 0, 0, DateTimeKind.Utc),
			WebsiteUrl = "https://powerplatformconf.com/",
			Country = "Las Vegas, USA"
		});
		return events;
	}

	private List<EventTrack> GetEventTracks(string tenantId, string eventId)
	{		
		List<EventTrack> tracks = new List<EventTrack>();
		tracks.Add(new EventTrack
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "1F4C65FF-19B6-48B7-9260-4343C2E390BA",
			EventId = eventId,
			Name = "Dynamics 365",
			Description = "Microsoft First Party Apps"
		});
		tracks.Add(new EventTrack
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "6F5992CC-B854-47D0-97D2-A4186769D915",
			EventId = eventId,
			Name = "Power Apps",
			Description = "Apps build with Power Apps"
		});
		tracks.Add(new EventTrack
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "8028A28B-9E5E-4BE5-97F1-E70AC422D9C1",
			EventId = eventId,
			Name = "Power Automate",
			Description = "Cloud and Desktop Automations"
		});
		tracks.Add(new EventTrack
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "5D03BE39-46E7-4C56-A4F2-1C7BD3877CE9",
			EventId = eventId,
			Name = "Copilot Studio",
			Description = "Copilot Studio and AI"
		});

		return tracks;		
	}

	private List<EventSession> GetEventSessions(string tenantId, string eventId)
	{
		List<EventSession> sessions = new List<EventSession>();

		// Fixed track IDs from GetEventTracks
		string track1 = "1F4C65FF-19B6-48B7-9260-4343C2E390BA"; // Dynamics 365
		string track2 = "6F5992CC-B854-47D0-97D2-A4186769D915"; // Power Apps
		string track3 = "8028A28B-9E5E-4BE5-97F1-E70AC422D9C1"; // Power Automate
		string track4 = "5D03BE39-46E7-4C56-A4F2-1C7BD3877CE9"; // Copilot Studio

		// Event-level PartitionKey
		string pk = $"{tenantId}-{eventId}";

		// Track 1 - 2 sessions
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "a3d9f5b2-7c1e-4b2e-8f0a-1a2b3c4d5e6f",
			EventdId = eventId,
			TrackId = track1,
			Title = "Dynamics 365: Modern Extensibility",
			Description = "Extending Dynamics with modern tools and patterns.",
			Speaker = "Alex Johnson"
		});
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "b4e0a6c3-8d2f-4c3f-9a1b-2b3c4d5e6f70",
			EventdId = eventId,
			TrackId = track1,
			Title = "Customer Engagement with Dynamics",
			Description = "Best practices for customer engagement scenarios.",
			Speaker = "Priya Patel"
		});

		// Track 2 - 1 session
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "c5f1b7d4-9e30-4d40-ab2c-3c4d5e6f7081",
			EventdId = eventId,
			TrackId = track2,
			Title = "Power Apps: Build Once, Run Anywhere",
			Description = "Create responsive apps quickly with Power Apps.",
			Speaker = "Lena Svensson"
		});

		// Track 3 - 3 sessions
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "d6a2c8e5-a041-4e51-bd3d-4d5e6f708192",
			EventdId = eventId,
			TrackId = track3,
			Title = "Intro to Power Automate",
			Description = "Automating business processes with Power Automate.",
			Speaker = "Tom Baker"
		});
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "e7b3d9f6-b152-4f62-ce4e-5e6f708192a3",
			EventdId = eventId,
			TrackId = track3,
			Title = "Desktop Automation Deep Dive",
			Description = "Using Power Automate Desktop for complex scenarios.",
			Speaker = "Sara Lee"
		});
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "f8c4e0a7-c263-5073-df5f-6f708192a3b4",
			EventdId = eventId,
			TrackId = track3,
			Title = "Process Mining and Automation",
			Description = "Combine process mining with automation for ROI.",
			Speaker = "Miguel Torres"
		});

		// Track 4 - 2 sessions
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "09a5f1b8-d374-5184-e06a-708192a3b4c5",
			EventdId = eventId,
			TrackId = track4,
			Title = "Introduction to Copilot Studio",
			Description = "Learn the basics of Copilot Studio and how to get started.",
			Speaker = "Daniel Laskewitz"
		});
		sessions.Add(new EventSession
		{
			PartitionKey = pk,
			RowKey = "1a06b2c9-e485-6295-f17a-8192a3b4c5d6",
			EventdId = eventId,
			TrackId = track4,
			Title = "Building Assistants with Copilot",
			Description = "Design patterns for building helpful assistants.",
			Speaker = "Nora White"
		});

		return sessions;
	}

	private List<EventForm> GetEventForms(string tenantId, string eventId)
	{
		List<EventForm> forms = new List<EventForm>();
		forms.Add(new EventForm
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "43BF8D7B-C0BF-4509-9AC1-D289285B414A",
			EventId = eventId,
			Name = "Sponsor Interest Form",
			Description = "Interested in supporting this amazing event? Let us know!",
			Format = @"{
                ""type"": ""object"",
                ""properties"": {
                  ""name"": {
                    ""type"": ""string"",
                    ""description"": ""Company Name"",
                    ""title"": ""Name""
                  },
                  ""email"": {
                    ""type"": ""string"",
                    ""description"": ""Contact E-Mail"",
                    ""title"": ""Email""
                  },
                  ""sponsorInterest"": {
                    ""type"": ""boolean"",
                    ""description"": ""Are you interested in Sponsoring?"",
                    ""title"": ""Sponsor Interest""
                  },
                  ""Budget"": {
                    ""type"": ""integer"",
                    ""description"": ""Maximum budget for the campaign"",
                    ""title"": ""Budget""
                  }
                }
              }"
		});
		forms.Add(new EventForm
		{
			PartitionKey = $"{tenantId}-{eventId}",
			RowKey = "0B7F43EA-38CD-4420-A0A7-1FC0A4BE499B",
			EventId = eventId,
			Name = "General Event Feedback",
			Description = "Please let us know how you as an attendee liked our event",
			Format = @"{
                ""type"": ""object"",
                ""properties"": {
                  ""name"": {
                    ""type"": ""string"",
                    ""description"": ""Your Name"",
                    ""title"": ""Your Name""
                  },
                  ""ratingOverall"": {
                    ""type"": ""integer"",                   
                    ""description"": ""Your overall rating 1 - 5"",
                    ""title"": ""Overall Rating""
                  },
                  ""ratingContent"": {
                    ""type"": ""integer"",
                    ""description"": ""Your rating of the content 1-5"",
                    ""title"": ""Content Rating""
                  },
                  ""Comment"": {
                    ""type"": ""string"",
                    ""description"": ""Comment about experience"",
                    ""title"": ""Comment""
                  }
                }
              }"
		});

		return forms;
	}
}
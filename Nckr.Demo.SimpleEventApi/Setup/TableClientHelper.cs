using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Nckr.Demo.SimpleEventApi.Setup
{
    // Static helper with convenience methods for creating clients
    public static class TableClientHelper
    {
        public const string EventTableName = "event";
        public const string EventTrackTableName = "eventTrack";
        public const string EventSessionTableName = "eventSession";
        public const string WebhookTableName = "webhook";
        public const string EventFormTableName = "eventForm";
        public const string EventFormSubmissionTableName = "eventFormSubmission";

        public static TableServiceClient CreateTableServiceClient(string connectionString)
        {
            return new TableServiceClient(connectionString);
        }

        public static TableClient CreateTableClient(TableServiceClient serviceClient, string tableName)
        {
            return serviceClient.GetTableClient(tableName);
        }
    }

    // Container for the table clients
    public class TableClients
    {
        public TableClient Event { get; }
        public TableClient EventTrack { get; }
        public TableClient EventSession { get; }
        public TableClient Webhook { get; }
        public TableClient EventForm { get; }
        public TableClient EventFormSubmission { get; }

        public TableClients(TableClient @event, TableClient eventTrack, TableClient eventSession, TableClient webhook, TableClient eventForm, TableClient eventFormSubmission)
        {
            Event = @event;
            EventTrack = eventTrack;
            EventSession = eventSession;
            Webhook = webhook;
            EventForm = eventForm;
            EventFormSubmission = eventFormSubmission;
        }
    }

    // Simple factory interface for DI
    public interface ITableClientFactory
    {
        TableServiceClient CreateServiceClient();
        TableClients GetTableClients();
        Task EnsureTablesExistAsync();
        TableClient GetEventService();
        TableClient GetEventTrackService();
        TableClient GetEventSessionService();
        TableClient GetWebhookService();
        TableClient GetEventFormService();
        TableClient GetEventFormSubmissionService();

    }

    // Implementation that uses the static helper and IConfiguration
    public class TableClientFactory : ITableClientFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TableClientFactory> _logger;
        private readonly TableServiceClient _serviceClient;

        public TableClientFactory(IConfiguration configuration, ILogger<TableClientFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var connectionString = _configuration["AzureStorageConnectionString"] ?? string.Empty;
            _serviceClient = TableClientHelper.CreateTableServiceClient(connectionString);
        }

        public TableServiceClient CreateServiceClient() => _serviceClient;

        public TableClients GetTableClients()
        {
            var eventClient = GetEventService();
            var eventTrackClient = GetEventTrackService();
            var eventSessionClient = GetEventSessionService();
            var webhookClient = GetWebhookService();
            var eventFormClient = GetEventFormService();
            var eventFormSubmissionClient = GetEventFormSubmissionService();

            return new TableClients(eventClient, eventTrackClient, eventSessionClient, webhookClient, eventFormClient, eventFormSubmissionClient);
        }

        public TableClient GetEventService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.EventTableName);
        }

        public TableClient GetEventTrackService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.EventTrackTableName);
        }

        public TableClient GetEventSessionService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.EventSessionTableName);
        }

        public TableClient GetWebhookService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.WebhookTableName);
        }

        public TableClient GetEventFormService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.EventFormTableName);
        }

        public TableClient GetEventFormSubmissionService()
        {
            return TableClientHelper.CreateTableClient(_serviceClient, TableClientHelper.EventFormSubmissionTableName);
        }

        public async Task EnsureTablesExistAsync()
        {
            var clients = GetTableClients();

            await clients.Event.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.EventTableName);

            await clients.EventTrack.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.EventTrackTableName);

            await clients.EventSession.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.EventSessionTableName);

            await clients.Webhook.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.WebhookTableName);

            await clients.EventForm.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.EventFormTableName);

            await clients.EventFormSubmission.CreateIfNotExistsAsync();
            _logger.LogInformation("Ensured table '{table}' exists.", TableClientHelper.EventFormSubmissionTableName);

        }
    }
}

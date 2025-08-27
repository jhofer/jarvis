using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.Collections.Concurrent;

namespace jarvis.ApiService.Integrations.Registration
{
    public interface IIntegrationRepository
    {
        public IEnumerable<Integration> GetIntegrations(string userId);
        Integration GetIntegration(string userId, IntegrationType integrationType);

        void Save(Integration integration);
    }

    public enum IntegrationType
    {
        OneDrive
    }

    public record Integration : ITableEntity
    {
        public string PartitionKey { get => UserId; set { UserId = value; } }
        public string RowKey { get => AppId; set { AppId = value; } }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public required string AppId { get; set; }

        public IntegrationType IntegrationType { get; set; }
        public required string IntegrationName { get => IntegrationType.ToString(); set => IntegrationType = Enum.Parse<IntegrationType>(value); }
        public required string UserId { get; set; }

        public required string RefreshToken { get; set; }
    }

    public class IntegrationRepository : IIntegrationRepository
    {
        private TableClient tableClient;
        private TableItem container;

        public IntegrationRepository(TableServiceClient tableServiceClient)
        {
            container = tableServiceClient.CreateTableIfNotExists("integrations").Value;
            tableClient = tableServiceClient.GetTableClient("integrations");
        }

        public bool TryGetRefreshToken(string userId, out string? refreshTokens)
        {
            var row = tableClient.GetEntity<TableEntity>(userId, "refreshToken").Value;
            if (row == null)
            {
                refreshTokens = null;
                return false;
            }
            refreshTokens = row.GetString("token");
            return true;
        }



        public IEnumerable<Integration> GetIntegrations(string userId)
        {
            Pageable<Integration> queryResultsFilter = tableClient.Query<Integration>(filter: $"PartitionKey eq '{userId}'");

            List<Integration> integrations = queryResultsFilter.ToList();

            return integrations;
        }

        public void Save(Integration integration)
        {
            tableClient.UpsertEntity(integration);
        }

        public Integration GetIntegration(string userId, IntegrationType integrationType)
        {
            Integration integration = tableClient.GetEntity<Integration>(userId, integrationType.ToString());
            return integration;
        }
    }

}

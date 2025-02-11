using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace jarvis.ApiService.Integrations
{
    public interface IAccessRepository
    {
        bool TryGetRefreshToken(string userId, out string? refreshTokens);

        void SaveRefreshToken(string userId, string? refreshToken);
    }



    public class StorageAccessRepository : IAccessRepository
    {
        private TableClient tableClient;
        private TableItem container;

        public StorageAccessRepository(TableServiceClient tableServiceClient)
        {
            container = tableServiceClient.CreateTableIfNotExists("refreshTokens").Value;
            tableClient = tableServiceClient.GetTableClient("refreshTokens");
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

        public void SaveRefreshToken(string userId, string? refreshToken)
        {
            var tableEntity = new TableEntity(userId, "refreshToken")
            {
                { "token",refreshToken },
            };

            tableClient.UpsertEntity(tableEntity);
        }
    }
    internal class InMemoryAccessRepository : IAccessRepository
    {

        private readonly Dictionary<string, string> refreshTokens = new Dictionary<string, string>();
        public bool TryGetRefreshToken(string userId, out string? refreshToken)
        {
            return refreshTokens.TryGetValue(userId, out refreshToken);
        }

        public void SaveRefreshToken(string userId, string refreshToken)
        {
            refreshTokens[userId] = refreshToken;
        }
    }
}

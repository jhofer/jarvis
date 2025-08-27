using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFRenamer.Services
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
            this.container = tableServiceClient.CreateTableIfNotExists("refreshTokens").Value;
            this.tableClient = tableServiceClient.GetTableClient("refreshTokens");
        }
        public bool TryGetRefreshToken(string userId, out string? refreshTokens)
        {
            var row = this.tableClient.GetEntity<TableEntity>(userId, "refreshToken").Value;
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

using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;




public interface ICache
{
    void Add(string key, Object value);
    bool TryGet<T>(string key, out T? cachedValue);
}
public class Cache : ICache
{
    private TableItem container;
    private TableClient tableClient;
    private ILogger<Cache> logger;

    public Cache(TableServiceClient tableServiceClient, ILogger<Cache> logger)
    {
        this.container = tableServiceClient.CreateTableIfNotExists("chache").Value;
        this.tableClient = tableServiceClient.GetTableClient("chache");
        this.logger = logger;
    }
    public void Add(string key, object value)
    {
        var json = JsonConvert.SerializeObject(value);
        var entity = new TableEntity(key, key)
        {
            { "timeStamp", DateTime.UtcNow},
            { "value", json }
        };
        tableClient.UpsertEntity(entity);
        logger.LogTrace($"Added {key} to cache");
    }


    public bool TryGet<T>(string key, out T? cachedValue)
    {
        try
        {

            var entity = tableClient.GetEntity<TableEntity>(key, key);
            var row = entity?.Value;
            if (row == null)
            {
                logger.LogTrace($"Cache miss for {key}");
                cachedValue = default(T);
                return false;
            }

            var json = row.GetString("value");
            var timeString = row.GetDateTime("timeStamp");
            if (timeString < DateTime.UtcNow.AddMinutes(-5))
            {
                logger.LogTrace($"Cache entry for {key} expired");
                tableClient.DeleteEntity(key, key);
                cachedValue = default;
                return false;
            }
            var obj = JsonConvert.DeserializeObject<T>(json);
            if (obj == null)
            {
                logger.LogTrace($"Failed to deserialize {key}");
                cachedValue = default;
                tableClient.DeleteEntity(key, key);
                return false;
            }
            cachedValue = obj;
            return true;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            logger.LogTrace($"Cache miss for {key}");
            cachedValue = default(T);
            return false;
        }
    }
}

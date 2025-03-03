using StackExchange.Redis;
using System.Text.Json;

namespace jarvis.ApiService.Cache
{
    public static class RedisExtension
    {
        public static void SetObject(this IDatabase db, string key, object value)
        {
            var json = JsonSerializer.Serialize(value);
            db.StringSet(key, json);
        }

        public static T ObjectGetDelete<T>(this IDatabase db, string key) where T : class
        {
            var json = db.StringGetDelete(key);
            var obj = JsonSerializer.Deserialize<T>(json);
            return obj;
        }
    }
}

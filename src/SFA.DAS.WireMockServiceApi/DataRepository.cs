using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceApi
{
    public static class DataRepository
    {
        private static CloudTable _table;

        public static async Task CreateTableStorage()
        {
            await CreateTableAsync(Settings.StorageTableName);
        }

        private static async Task CreateTableAsync(string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(Settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();

            _table = tableClient.GetTableReference(tableName);
            if (await _table.CreateIfNotExistsAsync())
            {
                Console.WriteLine("Created Table named: {0}", tableName);
                Thread.Sleep(TimeSpan.FromSeconds(1)); // cause it's not fast enough
            }
            else
            {
                Console.WriteLine("Table {0} already exists", tableName);
            }
        }

        public class JsonData : TableEntity
        {
            public string HttpMethod { get; set; }
            public string Url { get; set; }
            public string Data { get; set; }

        }

        public static async Task<string> GetJsonData(HttpMethod method, string url)
        {
            var alldata = await GetEntitiesFromTable<JsonData>();
            var item = alldata.SingleOrDefault(x =>
                x.Url == Uri.UnescapeDataString(url) && x.HttpMethod.Equals(method.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return item?.Data;
        }

        public static async Task InsertOrReplace(HttpMethod method, string url, object data)
        {
            var record = new JsonData
            {
                PartitionKey = Settings.EnvironmentName,
                RowKey = $"{method}_{Uri.EscapeDataString(url)}",
                Url = Uri.UnescapeDataString(url),
                HttpMethod = method.ToString(),
                Data = JsonSerializer.Serialize(data)
            };

            var operation = TableOperation.InsertOrReplace(record);
            await _table.ExecuteAsync(operation);
        }

        private static async Task<IEnumerable<T>> GetEntitiesFromTable<T>()
            where T : ITableEntity, new()
        {
            TableQuerySegment<T> querySegment = null;
            var entities = new List<T>();
            var query = new TableQuery<T>();

            do
            {
                querySegment = await _table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                entities.AddRange(querySegment.Results);
            } while (querySegment.ContinuationToken != null);

            return entities;
        }

        public static async Task DropTableStorage()
        {
            await _table.DeleteIfExistsAsync();
        }

        public static async Task<IEnumerable<JsonData>> GetAll()
        {
            return await GetEntitiesFromTable<JsonData>();
        }
    }
}
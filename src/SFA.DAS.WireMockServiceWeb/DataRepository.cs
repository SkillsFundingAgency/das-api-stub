using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb
{
    public interface IDataRepository
    {
        Task<string> GetData(HttpMethod method, string url);
        Task InsertOrReplace(HttpMethod method, string url, object data);
        Task DropTableStorage();
        Task CreateTableStorage();
        Task<IEnumerable<DataRepository.JsonData>> GetAll();
    }

    public class DataRepository : IDataRepository
    {
        private readonly ApiStubOptions _options;
        private CloudTable _table;

        public DataRepository(IOptions<ApiStubOptions> options)
        {
            _options = options.Value;
            CreateTableStorage().ConfigureAwait(false);
        }

        public async Task CreateTableStorage()
        {
            await CreateTableAsync(_options.StorageTableName);
        }

        private async Task CreateTableAsync(string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(_options.StorageAccountConnectionString);
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

        public async Task<string> GetData(HttpMethod method, string url)
        {
            var alldata = await GetEntitiesFromTable<JsonData>();
            var item = alldata.SingleOrDefault(x =>
                x.Url == Uri.UnescapeDataString(url) && x.HttpMethod.Equals(method.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return item?.Data;
        }

        public async Task InsertOrReplace(HttpMethod method, string url, object data)
        {
            var record = new JsonData
            {
                PartitionKey = _options.EnvironmentName,
                RowKey = $"{method}_{Uri.EscapeDataString(url)}",
                Url = Uri.UnescapeDataString(url),
                HttpMethod = method.ToString(),
                Data = JsonSerializer.Serialize(data)
            };

            var operation = TableOperation.InsertOrReplace(record);
            await _table.ExecuteAsync(operation);
        }

        private async Task<IEnumerable<T>> GetEntitiesFromTable<T>()
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

        public async Task DropTableStorage()
        {
            await _table.DeleteIfExistsAsync();
        }

        public async Task<IEnumerable<JsonData>> GetAll()
        {
            return await GetEntitiesFromTable<JsonData>();
        }

    }
}
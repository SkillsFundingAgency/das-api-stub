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
        string GetData(HttpMethod method, string url);
        Task InsertOrReplace(HttpMethod method, string url, object data);
        Task DropTableStorage();
        Task CreateTableStorage();
        IEnumerable<DataRepository.JsonData> GetAll();
        Task Delete(HttpMethod method, string url);
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

        public IEnumerable<JsonData> GetAll()
        {
            return _table.ExecuteQuery(new TableQuery<JsonData>());
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

        public string GetData(HttpMethod method, string url)
        {
            var item = _table.ExecuteQuery(new TableQuery<JsonData>()).SingleOrDefault(x =>
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


        public async Task DropTableStorage()
        {
            await _table.DeleteIfExistsAsync();
        }

        public async Task Delete(HttpMethod method, string url)
        {
            var record = _table.ExecuteQuery(new TableQuery<JsonData>()).Single(x => string.Equals(x.Url, url));
            var operation = TableOperation.Delete(record);
            await _table.ExecuteAsync(operation);
        }

    }
}
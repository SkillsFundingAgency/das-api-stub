using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb
{
    public interface IDataRepository
    {
        string GetData(HttpMethod method, string url);
        Task InsertOrReplace(HttpMethod method, string url, object jsonData, HttpStatusCode httpStatusCode = HttpStatusCode.OK);
        Task DropTableStorage();
        Task CreateTableStorage();
        IEnumerable<DataRepository.MappingData> GetAll();
        Task Delete(HttpMethod method, string url);
        IEnumerable<DataRepository.MappingData> Find(string url);
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

        public IEnumerable<MappingData> GetAll()
        {
            return _table.ExecuteQuery(new TableQuery<MappingData>());
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

        public class MappingData : TableEntity
        {
            public string HttpMethod { get; set; }
            public string Url { get; set; }
            public string Data { get; set; }
            public int HttpStatusCode { get; set; }
        }

        public string GetData(HttpMethod method, string url)
        {
            var item = _table.ExecuteQuery(new TableQuery<MappingData>()).SingleOrDefault(x =>
                x.Url == Uri.UnescapeDataString(url) && x.HttpMethod.Equals(method.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return item?.Data;
        }

        public async Task InsertOrReplace(HttpMethod method, string url, object jsonData, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var record = new MappingData
            {
                PartitionKey = _options.EnvironmentName,
                RowKey = $"{method}_{Uri.EscapeDataString(url)}",
                Url = Uri.UnescapeDataString(url),
                HttpMethod = method.ToString(),
                Data = JsonSerializer.Serialize(jsonData),
                HttpStatusCode = (int)httpStatusCode
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
            var record = _table.ExecuteQuery(new TableQuery<MappingData>()).Single(x =>
                string.Equals(x.Url, url, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(x.HttpMethod, method.ToString(), StringComparison.InvariantCultureIgnoreCase));
            var operation = TableOperation.Delete(record);
            await _table.ExecuteAsync(operation);
        }

        public IEnumerable<MappingData> Find(string url)
        {
            var data = _table.ExecuteQuery(new TableQuery<MappingData>()).Where(x =>
                 x.Url.Contains(Uri.UnescapeDataString(url), StringComparison.InvariantCultureIgnoreCase)).ToList();

            return data;
        }
    }
}
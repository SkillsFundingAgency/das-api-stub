using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockService.Data
{
    public interface IDataRepository
    {
        Task<DataRepository.MappingData> Get(string httpMethod, string url);
        Task<DataRepository.MappingData> InsertOrReplace(string httpMethod, string url, object jsonData, HttpStatusCode httpStatusCode = HttpStatusCode.OK);
        Task DropTableStorage();
        Task CreateTableStorage();
        IEnumerable<DataRepository.MappingData> GetAll();
        Task<DataRepository.MappingData> Delete(string httpMethod, string url);
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

        public async Task<MappingData> Get(string httpMethod, string url)
        {
            return await Retrieve(httpMethod, url);
        }

        public async Task<MappingData> InsertOrReplace(string httpMethod, string url, object jsonData, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            var record = new MappingData
            {
                PartitionKey = _options.EnvironmentName,
                RowKey = MappingKey.For(httpMethod, url),
                Url = Uri.UnescapeDataString(url),
                HttpMethod = httpMethod,
                Data = JsonSerializer.Serialize(jsonData),
                HttpStatusCode = (int)httpStatusCode
            };

            var operation = TableOperation.InsertOrReplace(record);
            await _table.ExecuteAsync(operation);
            return record;
        }


        public async Task DropTableStorage()
        {
            await _table.DeleteIfExistsAsync();
        }

        public async Task<MappingData> Delete(string httpMethod, string url)
        {
            var record = await Retrieve(httpMethod, url);
            if (record == null) return null;

            var operation = TableOperation.Delete(record);
            await _table.ExecuteAsync(operation);
            return record;
        }

        // A point read on partition and row key, so the cost does not grow with the number of mappings in the table
        private async Task<MappingData> Retrieve(string httpMethod, string url)
        {
            var operation = TableOperation.Retrieve<MappingData>(_options.EnvironmentName, MappingKey.For(httpMethod, url));
            return (await _table.ExecuteAsync(operation)).Result as MappingData;
        }

        public IEnumerable<MappingData> Find(string url)
        {
            var data = _table.ExecuteQuery(new TableQuery<MappingData>()).Where(x =>
                 x.Url.Contains(Uri.UnescapeDataString(url), StringComparison.InvariantCultureIgnoreCase)).ToList();

            return data;
        }
    }
}

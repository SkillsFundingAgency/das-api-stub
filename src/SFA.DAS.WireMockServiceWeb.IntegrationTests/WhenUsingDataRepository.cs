using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockService.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenUsingDataRepository
    {
        private readonly string _tableName = $"Test{Guid.NewGuid():N}";
        private DataRepository _repository;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AzureStorageEmulatorManager.StartStorageEmulator();
            _repository = RepositoryFor("DEV");
            await _repository.CreateTableStorage();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _repository.DropTableStorage();
        }

        [Test]
        public async Task a_saved_mapping_is_read_back_by_an_equivalent_url()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", $"{path.ToUpperInvariant()}?b=2&a=1", "saved");

            var mapping = await _repository.Get("GET", $"{path.TrimStart('/')}?a=1&b=2");

            mapping.Data.Should().Be("\"saved\"");
        }

        [Test]
        public async Task saving_an_equivalent_url_replaces_the_existing_mapping()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", $"{path}?b=2&a=1", "first");
            await _repository.InsertOrReplace("get", $"{path.ToUpperInvariant()}?a=1&b=2", "second");

            _repository.GetAll().Count(mapping => mapping.Url.Contains(path, StringComparison.OrdinalIgnoreCase)).Should().Be(1);
            (await _repository.Get("Get", path + "?a=1&b=2")).Data.Should().Be("\"second\"");
        }

        [Test]
        public async Task deleting_by_an_equivalent_url_removes_the_mapping()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", $"{path}?b=2&a=1", "saved");

            await _repository.Delete("GET", $"{path.ToUpperInvariant()}?a=1&b=2");

            (await _repository.Get("Get", $"{path}?b=2&a=1")).Should().BeNull();
        }

        [Test]
        public async Task deleting_a_mapping_that_does_not_exist_returns_null()
        {
            var deleted = await _repository.Delete("Get", $"/{Guid.NewGuid()}");

            deleted.Should().BeNull();
        }

        [Test]
        public async Task a_mapping_saved_for_one_environment_is_not_read_by_another()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", path, "saved");

            (await RepositoryFor("AT").Get("Get", path)).Should().BeNull();
        }

        private DataRepository RepositoryFor(string environmentName)
        {
            return new DataRepository(Options.Create(new ApiStubOptions
            {
                EnvironmentName = environmentName,
                StorageTableName = _tableName
            }));
        }
    }
}

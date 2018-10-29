using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosSelectManyRepro
{
    public class Class
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    public class Pupil
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Class> Classes { get; set; }
    }

    public class Container
    {
        [JsonProperty(PropertyName = "id")]
        public string Id => Pupil.Id.ToString();

        public Pupil Pupil { get; set; }

        public DateTime ModifiedAt { get; set; }
    }

    [TestFixture]
    public class CosmosShould
    {
        private static readonly Class CosmosDB101 = new Class { Id = Guid.Parse("501FFC8E-272D-4F26-BEBB-F5CE8CE1C095"), Name = "CosmosDB 101" };
        private static readonly Class AzureManagement = new Class { Id = Guid.Parse("E001BBA0-0B10-460A-B9DF-68616889CCB7"), Name = "Azure Management" };

        private static readonly Pupil Bob = new Pupil { Id = Guid.Parse("8CB7FA73-8BCE-41C5-97AD-9343C4D3F331"), Name = "Bob Maloogaloogaloogaloogaloooga", Classes = new[] { CosmosDB101, AzureManagement } };
        private static readonly Pupil Kristine = new Pupil { Id = Guid.Parse("0D34A2EC-4C62-4FCB-81F5-5FF86EC98ADC"), Name = "Kristine Kochanski", Classes = new[] { AzureManagement } };

        private static readonly Container BobContainer = new Container { Pupil = Bob, ModifiedAt = DateTime.UtcNow };
        private static readonly Container KristineContainer = new Container { Pupil = Kristine, ModifiedAt = DateTime.UtcNow };

        private DocumentClient _client;
        private Microsoft.Azure.Documents.Database _database;
        private Microsoft.Azure.Documents.DocumentCollection _collection;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _client = new DocumentClient(new Uri("https://localhost:8081"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            
            var database = await _client.CreateDatabaseIfNotExistsAsync(new Microsoft.Azure.Documents.Database { Id = "CosmosSelectManyRepro" });
            _database = database.Resource;

            var collection = await _client.CreateDocumentCollectionIfNotExistsAsync(_database.SelfLink, new Microsoft.Azure.Documents.DocumentCollection { Id = "Documents" });
            _collection = collection.Resource;

            await _client.UpsertDocumentAsync(_collection.SelfLink, BobContainer);
            await _client.UpsertDocumentAsync(_collection.SelfLink, KristineContainer);
        }

        [Test]
        public void ShouldBeAbleToQueryByClassId()
        {
            /*
             * Generates: SELECT VALUE root["Pupil"] FROM root JOIN class IN root["Classes"] WHERE (class["Id"] = "501ffc8e-272d-4f26-bebb-f5ce8ce1c095")
             * Should be: SELECT VALUE root["Pupil"] FROM root JOIN class IN root["Pupil"]["Classes"] WHERE (class["Id"] = "501ffc8e-272d-4f26-bebb-f5ce8ce1c095")
             */
            var query = _client
                .CreateDocumentQuery<Container>(_collection.SelfLink)
                .Select(container => container.Pupil)
                .SelectMany(pupil => pupil.Classes
                    .Where(@class => @class.Id == CosmosDB101.Id)
                    .Select(@class => pupil));

            var actual = query.ToArray();

            var expected = new[] { Bob };

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}

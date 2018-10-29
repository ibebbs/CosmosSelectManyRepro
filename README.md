# CosmosSelectManyRepro
Reproduction of [SelectMany doesn't work after Select](https://github.com/Azure/azure-cosmosdb-dotnet/issues/91#issuecomment-432037836) issue.

# Environment
Nunit test (with VS Test adapter) which uses the [CosmosDB local emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)

# Issue
The query:

```
var query = _client
  .CreateDocumentQuery<Container>(_collection.SelfLink)
  .Select(container => container.Pupil)
  .SelectMany(pupil => pupil.Classes
    .Where(@class => @class.Id == CosmosDB101.Id)
    .Select(@class => pupil));
```

Generates: `SELECT VALUE root["Pupil"] FROM root JOIN class IN root["Classes"] WHERE (class["Id"] = "501ffc8e-272d-4f26-bebb-f5ce8ce1c095")`

But should generate: `SELECT VALUE root["Pupil"] FROM root JOIN class IN root["Pupil"]["Classes"] WHERE (class["Id"] = "501ffc8e-272d-4f26-bebb-f5ce8ce1c095")`

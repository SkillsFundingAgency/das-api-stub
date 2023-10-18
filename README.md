# das-api-stub
[![Build Status](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_apis/build/status/Shared%20Services/das-api-stub?repoName=SkillsFundingAgency%2Fdas-api-stub&branchName=main)](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_build/latest?definitionId=2316&repoName=SkillsFundingAgency%2Fdas-api-stub&branchName=main)

Licensed under the [MIT license](https://github.com/SkillsFundingAgency/das-employer-incentives/blob/master/LICENSE)

#### Requirements
* DotNet Core 3.1 and any supported IDE for DEV running
* Azure Storage Account

## About

DAS API Stub is a<a href="https://github.com/WireMock-Net/WireMock.Net" target="_blank"> WireMock.Net</a> based API server for enabling substitution of RESTful dependee API endpoints. The Stub mappings are saved in Azure Storage and are automatically synchronised with in-memory WireMock service. Deployed solution consists of two layers:
* SFA.DAS.WireMockServiceApi - WireMock Web App
* SFA.DAS.WireMockServiceWeb - .NET Core Web API with Swagger UI front end for managing API Stub mappings

### Supported HTTP Methods
* Get
* Put
* Delete
* Post
* Head
* Trace
* Patch
* Connect
* Options
* Custom

### Supported HTTP Status codes
* Continue = 100
* SwitchingProtocols = 101
* Processing = 102
* EarlyHints = 103
* OK = 200 (Default)
* Created = 201
* Accepted = 202
* NonAuthoritativeInformation = 203
* NoContent = 204
* ResetContent = 205
* PartialContent = 206
* MultiStatus = 207
* AlreadyReported = 208
* IMUsed = 226
* Ambiguous = 300
* MultipleChoices = 300
* Moved = 301
* MovedPermanently = 301
* Found = 302
* Redirect = 302
* RedirectMethod = 303
* SeeOther = 303
* NotModified = 304
* UseProxy = 305
* Unused = 306
* RedirectKeepVerb = 307
* TemporaryRedirect = 307
* PermanentRedirect = 308
* BadRequest = 400
* Unauthorized = 401
* PaymentRequired = 402
* Forbidden = 403
* NotFound = 404
* MethodNotAllowed = 405
* NotAcceptable = 406
* ProxyAuthenticationRequired = 407
* RequestTimeout = 408
* Conflict = 409
* Gone = 410
* LengthRequired = 411
* PreconditionFailed = 412
* RequestEntityTooLarge = 413
* RequestUriTooLong = 414
* UnsupportedMediaType = 415
* RequestedRangeNotSatisfiable = 416
* ExpectationFailed = 417
* MisdirectedRequest = 421
* UnprocessableEntity = 422
* Locked = 423
* FailedDependency = 424
* UpgradeRequired = 426
* PreconditionRequired = 428
* TooManyRequests = 429
* RequestHeaderFieldsTooLarge = 431
* UnavailableForLegalReasons = 451
* InternalServerError = 500
* NotImplemented = 501
* BadGateway = 502
* ServiceUnavailable = 503
* GatewayTimeout = 504
* HttpVersionNotSupported = 505
* VariantAlsoNegotiates = 506
* InsufficientStorage = 507
* LoopDetected = 508
* NotExtended = 510
* NetworkAuthenticationRequired = 511

### Supported response body formats
* JSON

## Local running
Start `AzureStorageEmulator.exe` or `Azurite` (as Administrator) and run solution with 2 startup projects: `SFA.DAS.WireMockServiceApi` & `SFA.DAS.WireMockServiceWeb`

### Required configuration
#### SFA.DAS.WireMockServiceWeb
```json
"ApiStubSettings": {
   "WireMockServiceApiBaseUrl": "http://localhost:8089",
   "EnvironmentName": "DEV",
   "StorageAccountConnectionString": "UseDevelopmentStorage=true"
}
```
#### SFA.DAS.WireMockServiceApi
```json
"WireMockServerSettings": {
  "Port": 8089,
  "StartAdminInterface": true
}
```
### Running tests
All tests are intergration tests and require a local instance of Azure Storage Emulator or Azurite (run as Administrator). Tests can be run using dotnet-cli from the `das-api-stub\src` directory using command: `dotnet test -v=normal`

### WebAPI operations
The following operations are available in the front-end layer for managing API stub mappings:
#### GET - /api-stub/database
Retrieves all mappings from Azure table storage
#### GET - /api-stub/wiremock
Retrieves all mappings from in-memory WireMock service
#### GET - /api-stub/find
Searches all mappings by partial or full URL
#### GET - /api-stub/refresh
Synchonises mappings stored in Azure table storage with WireMock service. Should be used when mappings are added directly into the table storage for synchornisation. Also used in release pipe-line in a post-deployment step.
#### POST - /api-stub/save
Adds new or updates existing API stub mapping using URL input parameter as a key. If update is successful, automatically synchronises WireMock service with Azure storage mappings.
#### DELETE - /api-stub/delete
Deletes existing API stub mapping using URL input parameter as a key. If deletion is successful, automatically synchronises WireMock service with Azure storage mappings.

### Sample usage (C#)
##### Adding a HTTP GET mapping with 200 (OK) response code and JSON response body
```csharp
public async Task SetupResponse(long uln, long ukprn, LearnerSubmissionDto expectedResponse)
{
    var stringContent = new StringContent(JsonConvert.SerializeObject(expectedResponse), Encoding.UTF8, "application/json");
    var url = WebUtility.UrlEncode($"/learner-match/api/v1/{ukprn}/{uln}?");
    var response = await httpClient.PostAsync($"{stub_base_url}/api-stub/save?httpMethod=Get&url={url}", stringContent);
    response.EnsureSuccessStatusCode();
}
```

##### Deleting a mapping
```csharp
public async Task DeleteMapping(long uln, long ukprn)
{
    var url = WebUtility.UrlEncode($"/learner-match/api/v1/{ukprn}/{uln}?");
    var response = await httpClient.DeleteAsync($"{stub_base_url}/api-stub/delete?httpMethod=Get&url={url}");
    response.EnsureSuccessStatusCode();
}
```

##### Adding a HTTP POST mapping with 202 (Accepted) response code and empty response body
```csharp
public async Task SetupAcceptAllRequests()
{
    const string url = "/businesscentral/payments/requests?api-version=2020-10-01";
    var nullContent = new StringContent("{}", Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync($"{baseUrl}/api-stub/save?httpMethod=Post&url={WebUtility.UrlEncode(url)}&httpStatusCode=202", nullContent);
    response.EnsureSuccessStatusCode();
}
```

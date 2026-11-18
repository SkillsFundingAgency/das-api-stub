# das-api-stub
[![Build Status](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_apis/build/status/Shared%20Services/das-api-stub?repoName=SkillsFundingAgency%2Fdas-api-stub&branchName=main)](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_build/latest?definitionId=2316&repoName=SkillsFundingAgency%2Fdas-api-stub&branchName=main)

Licensed under the [MIT license](https://github.com/SkillsFundingAgency/das-employer-incentives/blob/master/LICENSE)

#### Requirements
* DotNet Core 3.1 and any supported IDE for DEV running
* Azure Storage Account

## About

DAS API Stub substitutes RESTful APIs that a service depends on, returning canned responses that tests and testers register. Mappings are saved in Azure Table Storage, and every stubbed request is answered by reading its mapping from the table, so no mappings are held in memory and any number of instances serve the same mappings as soon as they are saved. The solution consists of:
* SFA.DAS.WireMockServiceApi - the stub app that consumers call in place of the real API
* SFA.DAS.WireMockServiceWeb - .NET Core Web API with Swagger UI front end for managing stub mappings
* SFA.DAS.WireMockService.Data - the table storage repository and mapping key shared by both

The projects keep their WireMock names, but WireMock.Net is no longer used.

### How requests are matched
A request is answered by the mapping saved for the same HTTP method and URL. When comparing URLs:
* method and URL case are ignored
* a leading `/` and a trailing `?` are ignored
* percent-encoding is ignored, so a fully escaped URL matches its plain form
* query parameter order is ignored, but the set of parameters must match exactly: a request with extra parameters does not match

Requests without a matching mapping get `404` with `{"Status":"No matching mapping found"}`.

Because the whole URL is passed to `/api-stub/save` as a query parameter, escape it (for example with `Uri.EscapeDataString`) when it has a query string of its own. Otherwise everything after its first `&` is read as a separate parameter of the save request and lost.

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
Start `Azurite` (or `AzureStorageEmulator.exe`) and run the solution with 2 startup projects: `SFA.DAS.WireMockServiceApi` & `SFA.DAS.WireMockServiceWeb`. The stub app listens on `http://localhost:8089`.

### Required configuration
Both apps read the same settings, and must use the same `EnvironmentName`, which is the table partition mappings are saved in and served from.
```json
"ApiStubSettings": {
   "EnvironmentName": "DEV",
   "StorageAccountConnectionString": "UseDevelopmentStorage=true"
}
```
`SFA.DAS.WireMockServiceApi` also accepts `"LogRequestBodies": true` to log the body of every stubbed request.

### Running tests
All tests are integration tests and require a local instance of Azurite or Azure Storage Emulator. Tests can be run using dotnet-cli from the `das-api-stub\src` directory using command: `dotnet test -v=normal`

### WebAPI operations
The following operations are available in the front-end layer for managing API stub mappings:
#### GET - /api-stub/database
Retrieves all mappings from Azure table storage
#### GET - /api-stub/find
Searches all mappings by partial or full URL
#### GET - /api-stub/refresh
Does nothing and returns `200`. Kept because the release pipeline's post-deploy step calls it; mappings are read from table storage on every request, so there is nothing to synchronise.
#### POST - /api-stub/save
Adds a new mapping or replaces the existing one for the same method and URL. It is served immediately by every instance of the stub app.
#### DELETE - /api-stub/delete
Deletes the mapping for the method and URL. Returns `404` if there is no such mapping.

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

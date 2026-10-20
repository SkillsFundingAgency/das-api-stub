using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.WireMockService.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceApi
{
    // Answers every request from the mapping saved in table storage for its method and URL.
    // Nothing is held in memory, so any number of instances serve the same mappings as soon as they are saved.
    public class StubResponseMiddleware
    {
        private const string NoMatchingMappingBody = "{\"Status\":\"No matching mapping found\"}";

        private readonly IDataRepository _repository;
        private readonly ILogger<StubResponseMiddleware> _logger;
        private readonly bool _logRequestBodies;

        // Terminal middleware: there is no next delegate to call, but ASP.NET Core requires the constructor to accept it
        public StubResponseMiddleware(RequestDelegate next, IDataRepository repository, ILogger<StubResponseMiddleware> logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _logRequestBodies = configuration.GetValue<bool>("LogRequestBodies");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var url = $"{request.Path}{request.QueryString}";

            var mapping = await _repository.Get(request.Method, url);

            if (mapping == null)
            {
                await WriteResponse(context.Response, (int)HttpStatusCode.NotFound, NoMatchingMappingBody);
            }
            else
            {
                await WriteResponse(context.Response, mapping.HttpStatusCode, mapping.Data);
            }

            await LogRequest(request, url, context.Response.StatusCode, mapping != null);
        }

        private static async Task WriteResponse(HttpResponse response, int statusCode, string body)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            // Kestrel rejects a body on these status codes
            if (statusCode == (int)HttpStatusCode.NoContent || statusCode == (int)HttpStatusCode.NotModified || statusCode < 200) return;

            await response.WriteAsync(body);
        }

        private async Task LogRequest(HttpRequest request, string url, int statusCode, bool matched)
        {
            if (!_logRequestBodies)
            {
                _logger.LogInformation("{Method} {Url} -> {StatusCode} (matched: {Matched})", request.Method, url, statusCode, matched);
                return;
            }

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("{Method} {Url} -> {StatusCode} (matched: {Matched}) request body: {Body}", request.Method, url, statusCode, matched, body);
        }
    }
}

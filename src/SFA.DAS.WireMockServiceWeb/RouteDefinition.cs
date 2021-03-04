using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SFA.DAS.WireMockServiceWeb
{
    internal class RouteDefinition
    {
        public RouteDefinition(DataRepository.MappingData route)
        {
            Data = route.Data;
            HttpMethod = route.HttpMethod;
            BaseUrl = route.Url.Split("?").First();
            SetQueryStringParameters(route);
            HttpStatusCode = route.HttpStatusCode;
        }

        private void SetQueryStringParameters(DataRepository.MappingData route)
        {
            if (route.Url.IndexOf("?", StringComparison.Ordinal) < 0) return;
            var query = HttpUtility.ParseQueryString(route.Url.Split("?").Last());
            foreach (var key in query.AllKeys.Where(k => k != null))
            {
                Parameters.Add(key, query[key]);
            }
        }

        public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
        public string HttpMethod { get; }
        public string Data { get; }
        public string BaseUrl { get; }
        public int HttpStatusCode { get; }
    }
}
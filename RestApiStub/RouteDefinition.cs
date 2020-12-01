using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestApiStub
{
    internal class RouteDefinition
    {
        public RouteDefinition(DataRepository.JsonData route)
        {
            Data = route.Data;
            HttpMethod = route.HttpMethod;
            BaseUrl = route.Url.Split("?").First();
            SetQueryStringParameters(route);
        }

        private void SetQueryStringParameters(DataRepository.JsonData route)
        {
            if (route.Url.IndexOf("?", StringComparison.Ordinal) < 0) return;
            var query = HttpUtility.ParseQueryString(route.Url.Split("?").Last());
            Parameters = new Dictionary<string, string>();
            foreach (var key in query.AllKeys.Where(k => k != null))
            {
                Parameters.Add(key, query[key]);
            }
        }

        public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public string HttpMethod { get; set; }
        public string Data { get; set; }
        public string BaseUrl { get; set; }
    }
}
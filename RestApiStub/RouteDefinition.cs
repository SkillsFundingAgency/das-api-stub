using System.Collections.Generic;
using System.Linq;

namespace RestApiStub
{
    internal class RouteDefinition
    {
        public RouteDefinition(DataRepository.JsonData route)
        {
            Data = route.Data;
            HttpMethod = route.HttpMethod;
            BaseUrl = route.Url.Split("?").First();
            var query = route.Url.Split("?").Last();

            foreach (var p in query.Split("&"))
            {
                Parameters.Add(p.Split("=").First(), p.Split("=").Last());
            }
        }

        public string HttpMethod { get; set; }
        public string Data { get; set; }
        public string BaseUrl { get; set; }
        public IDictionary<string, string> Parameters = new Dictionary<string, string>();
    }
}
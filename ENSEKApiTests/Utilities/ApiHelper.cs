using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace ENSEKApiTests.Utilities
{
    public static class ApiHelper
    {
        private static readonly string _baseUrl;

        static ApiHelper()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _baseUrl = config["ApiSettings:BaseUrl"];
        }

        public static RestResponse ExecuteRequest(
            string endpoint,
            Method httpMethod,
            object body = null,
            Dictionary<string, string> headers = null)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest(endpoint, httpMethod);

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            // Add body if present
            if (body != null)
            {
                request.AddJsonBody(body);
            }

            return client.Execute(request);
        }
    }
}

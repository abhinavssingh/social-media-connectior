using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace DataConnector.Intg.TestHelper
{
    public abstract class TestFactory
    {
        protected TraceWriter log = new VerboseDiagnosticsTraceWriter();
        public static IEnumerable<object[]> Data()
        {
            return new List<object[]>
            {
                new object[] { "name", "Bill" },
                new object[] { "name", "Paul" },
                new object[] { "name", "Steve" }

            };
        }

        private static Dictionary<string, StringValues> CreateDictionary(string key, string value)
        {
            var qs = new Dictionary<string, StringValues>
            {
                { key, value }
            };
            return qs;
        }

        //public static HttpRequest CreateHttpRequest(string queryStringKey, string queryStringValue)
        //{
        //    var request = new HttpRequest()
        //    {
        //        Query = new QueryCollection(CreateDictionary(queryStringKey, queryStringValue))
        //    };
        //    return request;
        //}

        public HttpRequestMessage HttpRequestSetup(string requestURL, Dictionary<String, StringValues> query, string body)
        {
            var request = new HttpRequestMessage();
            if (!string.IsNullOrEmpty(body))
            {
                request.Method = HttpMethod.Post;
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                request.Method = HttpMethod.Get;                
                int i = 1;
                foreach (var q in query)
                {
                    requestURL = i == 1 ? string.Format("{0}?" + "{1}={2}", requestURL, q.Key, q.Value) : string.Format("{0}&" + "{1}={2}", requestURL, q.Key, q.Value);                    
                    i++;
                }
            }
            request.RequestUri = new Uri(requestURL);
            return request;
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;

            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }

            return logger;
        }
    }

    public class VerboseDiagnosticsTraceWriter : TraceWriter
    {
        public VerboseDiagnosticsTraceWriter() : base(TraceLevel.Verbose)
        {

        }
        public override void Trace(TraceEvent traceEvent)
        {
            try
            {
                Debug.WriteLine(traceEvent.Message);
            }
            catch(Exception)
            {
                throw;
            }
        }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SalesforceClient
{
    public class SalesforceClientUtility: IDisposable
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthToken { get; set; }
        public string InstanceUrl { get; set; }

        public string LoginEndpoint { get; set; }
        public string ApiEndpoint { get; set; }
        public string BulkApiEndpoint { get; set; }
        public const string ProxyUrl = "http://abcproxy.com:8080/"; //you may need a proxy if you are operating behind a corporate firewall

        static SalesforceClientUtil()
        {
            // SF requires TLS 1.1 or 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
        }

        public void Login()
        {
            String jsonResponse;
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                var request = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "password"},
                {"client_id", ClientId},
                {"client_secret", ClientSecret},
                {"username", Username},
                {"password", Password + Token}
            }
                );
                request.Headers.Add("X-PrettyPrint", "1");
                var response = client.PostAsync(LoginEndpoint, request).Result;
                jsonResponse = response.Content.ReadAsStringAsync().Result;
            }
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);
            AuthToken = values["access_token"];
            InstanceUrl = values["instance_url"];
        }

        //This queries an existing Salesforce records
        public string QueryEndpoints()
        {
            using (var client = new HttpClient())
            {
                string restQuery = InstanceUrl + ApiEndpoint;
                var request = new HttpRequestMessage(HttpMethod.Get, restQuery);
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This profiles or describes an existing Salesforce object
        public string Describe(string sObject)
        {
            using (var client = new HttpClient())
            {
                string restQuery = InstanceUrl + ApiEndpoint + "sobjects/" + sObject;
                var request = new HttpRequestMessage(HttpMethod.Get, restQuery);
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This queries an existing Salesforce records by passing a soql string
        public string Query(string soqlQuery)
        {
            using (var client = new HttpClient())
            {
                string restRequest = InstanceUrl + ApiEndpoint + "query/?q=" + soqlQuery;
                var request = new HttpRequestMessage(HttpMethod.Get, restRequest);
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }
        
        //This updates an existing Salesforce record
        public string Put(string objectName, string recordId)
        {
            using (var client = new HttpClient())
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                httpBody.Add("Status", "Active");
                string restRequest = InstanceUrl + ApiEndpoint + "sobjects/" + objectName + "/" + recordId;
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), restRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(httpBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This creates a Salesforce job for asynchronous batch data processing.
        public string CreateJob(string operation, string objectName, string externalIdFieldName, string columnDelimiter,string query="")
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                if(!string.IsNullOrEmpty(objectName))
                {
                    httpBody.Add("object", objectName);
                }
                if (!string.IsNullOrEmpty(query))
                {
                    httpBody.Add("query", query);
                }

                httpBody.Add("contentType", "CSV");
                httpBody.Add("columnDelimiter", columnDelimiter);
                //httpBody.Add("lineEnding", "CRLF");
                if (!string.IsNullOrEmpty(externalIdFieldName))
                {
                    httpBody.Add("externalIdFieldName", externalIdFieldName);
                }
                httpBody.Add("operation", operation);
                string restRequest = InstanceUrl + BulkApiEndpoint;
                var request = new HttpRequestMessage(new HttpMethod("POST"), restRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(httpBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("ContentType", "application/json");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This aborts an existing Salesforce job.
        public string AbortJob(string jobId)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                httpBody.Add("state", "Aborted");
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/";
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), restRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(httpBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("ContentType", "application/json;");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This closes an existing Salesforce job.
        public string CloseJob(string jobId)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                httpBody.Add("state", "UploadComplete");
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/";
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), restRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(httpBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("ContentType", "application/json;");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This starts a Salesforce batch data update job.
        public string BatchPut(string jobId, string csvContent)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/batches";
                StringContent content = new StringContent(csvContent, Encoding.UTF8, "text/csv");
                content.Headers.ContentType.CharSet = string.Empty;
                var request = new HttpRequestMessage(new HttpMethod("PUT"), restRequest)
                {
                    Content = content
                };
                request.Headers.Add("ContentType", "text/csv");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This checks the status of a Salesforce batch job.
        public string GetJobSuccessfulRecordResults(string jobId)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/successfulResults";
                var request = new HttpRequestMessage(new HttpMethod("GET"), restRequest);
                request.Headers.Add("ContentType", "text/csv");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This retrieves logs of a failed Salesforce batch job.
        public string GetJobFailedRecordResults(string jobId)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/failedResults";
                var request = new HttpRequestMessage(new HttpMethod("GET"), restRequest);
                request.Headers.Add("ContentType", "text/csv");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This returns results of a successful Salesforce batch job.
        public HttpResponseMessage GetJobQueryRecordResults(string jobId, string locator, string maxRecords)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId + "/results";
                if(locator!=null && !string.IsNullOrEmpty(locator) && locator != "null")
                {
                    restRequest += "?locator=" + locator;
                }
                if (!string.IsNullOrEmpty(maxRecords))
                {
                    if (!string.IsNullOrEmpty(locator))
                    {
                        restRequest += "&";
                    }
                    else
                    {
                        restRequest += "?";
                    }
                       
                    restRequest += "maxRecords=" + maxRecords;
                }
                var request = new HttpRequestMessage(new HttpMethod("GET"), restRequest);
                request.Headers.Add("ContentType", "text/csv");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Headers.Add("X-PrettyPrint", "1");
                HttpResponseMessage response = client.SendAsync(request).Result;
                return response;
                //Console.WriteLine(response.Headers);
                //return response.Content.ReadAsStringAsync().Result;
            }
        }

        //This describes or profiles an existing Salesforce job.
        public string GetJobInfo(string jobId)
        {
            HttpClientHandler proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
            if (!String.IsNullOrEmpty(ProxyUrl))
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyUrl);
            }
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                Dictionary<string, string> httpBody = new Dictionary<string, string>();
                string restRequest = InstanceUrl + BulkApiEndpoint + "/" + jobId;
                var request = new HttpRequestMessage(new HttpMethod("GET"), restRequest);
                request.Headers.Add("ContentType", "application/json");
                request.Headers.Add("Authorization", "Bearer " + AuthToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
               
                var response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public void Dispose()
        {
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

    }
}

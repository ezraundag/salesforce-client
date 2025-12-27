using Newtonsoft.Json;
using SalesforceClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Services.Salesforce
{
    public class SalesforceService : ISalesforceService
    {
        public string Username;
        public string Password;
        public string Token;
        public string ClientId;
        public string ClientSecret;
        public string LoginEndpoint;
        public string ApiEndpoint;
        public string BulkApiEndpointIngest;
        public string BulkApiEndpointQuery;

        public CreateJobHttpResponse CreateQueryJob(string soql,string actionName,string objectName)
        {
            CreateJobHttpResponse jobHttpResponse = new CreateJobHttpResponse();
            string successsfulResultsResponse = string.Empty;
            try
            {
                using (SalesforceClientUtil client = new SalesforceClientUtil
                {
                    Username = Username,
                    Password = Password,
                    Token = Token,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    LoginEndpoint = LoginEndpoint,
                    ApiEndpoint = ApiEndpoint,
                    BulkApiEndpoint = BulkApiEndpointQuery
                })
                {
                    client.Login();

                    string createJobResponse = client.CreateJob(actionName, string.Empty, string.Empty, "PIPE",soql);
                    try
                    {
                        jobHttpResponse = JsonConvert.DeserializeObject<CreateJobHttpResponse>(createJobResponse);
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("serialization prob encountered. try serialing again with apierror object >> {0}", createJobResponse);
                        List<APIErrorHttpResponse> apiErrorHttpResponses = JsonConvert.DeserializeObject<List<APIErrorHttpResponse>>(createJobResponse);
                        Console.WriteLine("error code >> {0}", apiErrorHttpResponses.First().errorCode);
                        Console.WriteLine("error message >> {0}", apiErrorHttpResponses.First().message);


                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw e;
            }
            return jobHttpResponse;
        }

        public CloseAbortJobHttpResponse pushObject(string objectName, string csv, string jobAction, string externalIdFieldName)
        {
            CloseAbortJobHttpResponse jobHttpResponse = new CloseAbortJobHttpResponse();
            try
            {
                using (SalesforceClientUtil client = new SalesforceClientUtil
                {
                    Username = Username,
                    Password = Password,
                    Token = Token,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    LoginEndpoint = LoginEndpoint,
                    ApiEndpoint = ApiEndpoint,
                    BulkApiEndpoint = BulkApiEndpointIngest
                }) {
                    client.Login();

                    string createJobResponse = client.CreateJob(jobAction, objectName, externalIdFieldName, "PIPE");
                    try
                    {
                        CreateJobHttpResponse createJobHttpResponse = JsonConvert.DeserializeObject<CreateJobHttpResponse>(createJobResponse);
                        if (!string.IsNullOrEmpty(createJobHttpResponse.id) && createJobHttpResponse.state == "Open")
                        {
                            string jobId = createJobHttpResponse.id;
                            string batchputResponse = client.BatchPut(jobId, csv);
                            
                            try
                            {


                                if (string.IsNullOrEmpty(batchputResponse))
                                {
                                    string closeJobResponse = client.CloseJob(jobId);
                                    jobHttpResponse = JsonConvert.DeserializeObject<CloseAbortJobHttpResponse>(closeJobResponse);
                                }
                            }
                            catch (Exception err)
                            {
                                string abortResponse = client.AbortJob(jobId);
                                jobHttpResponse = JsonConvert.DeserializeObject<CloseAbortJobHttpResponse>(abortResponse);
                                Console.WriteLine(jobHttpResponse.state);

                                string body = "Service: SalesforceService.pushObject";
                                body += "<br/>";
                                body += err.Message;
                                body += "<br/>";
                                body += err.StackTrace;
                                Console.WriteLine(body);
                            }
                        };
                    } 
                    catch(Exception e)
                    {
                        Console.WriteLine("serialization prob encountered. try serialing again with apierror object >> {0}", createJobResponse);
                        List<APIErrorHttpResponse> apiErrorHttpResponses = JsonConvert.DeserializeObject<List<APIErrorHttpResponse>>(createJobResponse);
                        Console.WriteLine("error code >> {0}", apiErrorHttpResponses.First().errorCode);
                        Console.WriteLine("error message >> {0}", apiErrorHttpResponses.First().message);
                      

                    }
                }
            }
            catch (Exception e)
            {

                string subject = "SalesforceService.pushObject: Runtime Error";
                string body = "Service: SalesforceService.pushObject";
                body += "<br/>";
                body += e.Message;
                body += "<br/>";
                body += e.StackTrace;
                Console.WriteLine(body);
                throw e;
            }
            return jobHttpResponse;
        }

        public JobQueryResponse GetQueryJobResults(string jobId, string locator, string maxRecords)
        {
            JobQueryResponse jobQueryResponse = new JobQueryResponse();

            try
            {
                using (SalesforceClientUtil client = new SalesforceClientUtil
                {
                    Username = Username,
                    Password = Password,
                    Token = Token,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    LoginEndpoint = LoginEndpoint,
                    ApiEndpoint = ApiEndpoint,
                    BulkApiEndpoint = BulkApiEndpointQuery
                })
                {
                    client.Login();
                    HttpResponseMessage response = client.GetJobQueryRecordResults(jobId,locator,maxRecords);
                    if (response.Headers.TryGetValues("Sforce-NumberOfRecords", out IEnumerable<string> values))
                    {
                        string numRecord = values.First();
                        jobQueryResponse.numRecords = Convert.ToInt32(numRecord);

                    }
                    if (response.Headers.TryGetValues("Sforce-Locator", out IEnumerable<string> values2))
                    {
                        string locatorX = values2.First();
                        if(!string.IsNullOrEmpty(locatorX) && locatorX!="null")
                        {
                            jobQueryResponse.locator = locatorX;
                        }
                        

                    }
                    jobQueryResponse.csv = response.Content.ReadAsStringAsync().Result;

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw e;
            }
            return jobQueryResponse;
        }

        
    }
}

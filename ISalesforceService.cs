using System;
using System.Collections.Generic;
using System.Text;

namespace Interfaces.Salesforce
{
    public interface ISalesforceService
    {
        string getJobStatusIngest(string jobId);
        string getJobStatusQuery(string jobId);

        CloseAbortJobHttpResponse pushObject(string objectName, string csv, string jobAction,string externalIdFieldName);
        CreateJobHttpResponse CreateQueryJob(string soql,string actionName,string objectName);
        JobQueryResponse GetQueryJobResults(string jobId, string locator, string maxRecords);

    }
}

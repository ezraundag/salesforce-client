This repo contains custom classes written in C# that utilizes Salesforce REST API endpoints for asynchronous data processing.

I wrote these classes to streamline external processing on large datasets with Salesforce.

**SalesforceClientUtility.cs**

This class contains functions that authenticates with Salesforce, queries, updates and starts a batch job for asynchronous data processing with Salesforce.

**SalesforceService.cs** 

This class  contains functions that invokes SalesforceClientUtility methods to execute queries, data updates and batch jobs for asynchronous data processing with Salesforce.

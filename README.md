# Keyfactor Command Sample Job Completion Handler

## Overview

The Keyfactor Command platform has an extension point that allows custom code to be executed after the completion
of orchestrator jobs.
This code is in the form of a .NET assembly that is installed Command server. 
This code receives basic information about the job (orchestrator that executed the job, job type, job identification,
success or failure, etc.) and can perform whatever server-side job post-processing is desired.

Typical use cases would be triggering external business workflow systems on success or failure of a orchestrator jobs.

This repository contains a sample job completion handler that traces out the job context information provided and
demonstrates making an API call back into the Command platform.

Job Completion Handlers were introduced in Command version 9.0, this sample has been tested with Command Version 10.3


## Getting Started

To use this sample you will need:
- An understanding of the Keyfactor Command platform, orchestrators, orchestrator extensions, certificate stores, and certificate store related jobs
- An understanding of C# and .NET development
- A working Keyfactor Command instance and administrative access to the server it is running on
- An installed, configured, and approved Universal Orchestrator Framework instance
- The "Windows Certificate" Universal Orchestrator Extension, installed into the Orchestrator Framework
- The corresponding "WinCert" certificate store type configured on the Command instance
- An instance of a "WinCert" certificate store with a scheduled inventory job
- Visual Studio (or other development environment that can build a .NET C# Assembly)

#### 1 - Get a Command environment up and running with an inventory job running on a certificate store

Job completion handlers execute after an orchestrator job completes, so to play with completion handlers, you will need some
job running on some orchestrator. Completion handlers are typically written to be specific to a job type and in the case
of certificate store jobs, also specific to a certificate store type.
This sample is specific to the "WinCert" certificate store type, which is used to manage certificates found in Local
Machine certificate stores on Windows Servers. If you don't already have the WinCert extension installed in your orchestrator, it
can be found at https://github.com/Keyfactor/iis-orchestrator. Any certificate store type may be used, but adjustments
to the sample will be necessary if it isn't "WinCert"

The details of getting your Command environment with a certificate store set up are beyond the scope of this documentation. 

#### 2 - Determine the Certificate Store Type Job IDs in your Command environment

Every job that can be sent to an Orchestrator has a "Job Type" that is identified by a GUID.
When a certificate store type is configured in Command, the corresponding job types related to that certificate store type
are dynamically created.
These job type GUIDs are unique to each Command instance.

When a completion handler is registered in the Command system, the registration includes a list of the job type GUIDs 
that the handler will handle.
Whenever a job completes, the Command platform checks to see if there are any job completion handlers registered to handle
that job type, and if so the handler is called.

Since this sample is specific to the WinCert store type, the specific WinCert related job GUIDs must be determined for 
your environment.

To retrieve the GUIDs, the `GET /CertificateStoreTypes/Name/` API endpoint may be used.

Using the Keyfactor API Reference and Utility, scroll down to the *CertificateStoreType* API, and select the
GET /CertificateStoreTypes/Name/{name} end point.
Enter "WinCert" the name parameter and click the `Try it out!` button to execute the API.

Alternatively, you could execute the following curl command:

`curl -X GET --header 'Accept: application/json' --header 'x-keyfactor-api-version: 1' --header 'x-keyfactor-requested-with: APIClient' 'https://{server}/Keyfactor/API/CertificateStoreTypes/Name/WinCert'`

Once the response is returned, scroll down to the end of the JSON result.
You will see a list of Job Types and their associated GUIDs which will look something like:

```
    "InventoryJobType": "49b3a17d-cada-4ec8-84c6-7719bf5beef3",
    "ManagementJobType": "4be14534-55b0-4cd7-9871-536b55b5e973",
    "EnrollmentJobType": "e868b3f8-9b6a-48b1-91c8-683d71d94f61"
```

Note that if you pick a different cert store type, you may see different job types as not all cert store types implement 
all of the possible job types. The WinCert store implements the above three job types. Your GUIDs will be different.

#### 3 - Build the sample extension

Add Keyfactor's GitHub NuGet package repository to your list of Visual Studio NuGet package sources.
You will need to create a GitHub personal access token and use it to authenticate to the GitHub package source.
[Instructions TBD]

Use Visual Studio to build the solution file in this repository.

Note that this extension will need to be compiled with the same version of the .NET Framework as is the Command 
instance it will be plugged into. For Keyfactor Command 10.3 and prior, you will need to target .NET Framework 4.7.2
(which this solution does target).
Future versions of Command are planed to be targeted with .NET Core and if this sample is built against future versions,
the target framework may need to be updated.

#### 4 - Copy the assembly to the Command Server

Copy the compiled assembly (`SampleJobCompletionHandler.dll`) to bin folder for the Orchestrator API endpoint on the 
Command Server. This is typically `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin `
    
For this sample only the above DLL needs to be copied to the target system.

In cases where your code may need additional dependent assemblies, make sure to only copy assemblies that are specific to your handler. 
Do not overwrite DLLs that ship with the Command platform. 
You will need to make sure that the handler references the same versions of libraries already in use in the WebAgentServices location.

#### 5 - Register the handler on the Command Server

Job completion handlers are registered via Unity.

Edit the web.config for the Orchestrator API endpoint on the Command Server. This is typically found at 
`C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`

Add the following new registration inside of `<unity><container>` along with the other <register ... /> items.
Use the job type GUIDs from your environment instead of the ones below.  
```
<register type="IOrchestratorJobCompleteHandler" mapTo="KFSample.SampleJobCompletionHandler, SampleJobCompletionHandler" name="SampleJobCompletionHandler">
    <property name="JobTypes" value="49b3a17d-cada-4ec8-84c6-7719bf5beef3,4be14534-55b0-4cd7-9871-536b55b5e973,e868b3f8-9b6a-48b1-91c8-683d71d94f61" />  <!-- Comma separated list of Job Type GUIDs to process -->
    <property name="FavoriteAnimal" value="Tiger" /> <!-- Sample parameter to pass into the handler. This parameter must be a public property on the class -->
</register>
```

Note that a broken Unity registration or a registration that points to an assembly that cannot be loaded can prevent
the Orchestrator API from operating and prevent all Orchestrators from contacting the platform. Be sure the check the
logs (see below) for proper operation any time the Unity registration or the corresponding Assemblies are changed.

#### 6 - Enable trace logging for the handler

To be able to see the trace log messages in the sample without having to enable trace level logging for the whole 
Orchestrator API endpoint, find the nlog config file for the Orchestrator API endpoint, usually located at:

`C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config`

and add the following rule inside the `<Rules>` section:

`<logger name="*.SampleJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>`

just before the default logging rule:

 `<logger name="*" minlevel="Info" writeTo="logfile" />`

#### 7 - Restart IIS

This Unity registration will require that the web server is restarted, which can be done by running the iisreset command.

At this point trace messages should appear in the Orchestrator API endpoint logs whenever a WinCert store type job
is completed by an orchestrator. By default the logs will be at `C:\Keyfactor\logs\Command_OrchestratorsAPI_Log.txt`

 *** Needs cleanup below ***


# Understanding the Sample

Examine the sample code for a detailed understanding of how the handler works, but a higher level overview is described here.

#### Orchestrator Job Flow

For background information, orchestrator jobs are processed by Command as follows:

- A job is scheduled on the Command platform (and targeted at a specific Orchestrator).
- Orchestrators periodically check in with the Command platform to see if there is any work requested. When they check in they receive a list of jobs and requested times to run those jobs.
- When the orchestrator thinks it's time to do a job that it was asked to do, it asks the Command platform for the job details.
- The Command platform provides the job details and the orchestrator executes the job.
- The Orchestrator returns the results of the job and the completion status to the Command platform.
- The Command platform stores any job related data (such as inventory results) and then records the completion status of the job
- The Command platform looks for any registered job completion handlers that match the job type and executes them.
- (We are here)

While this sample discusses jobs that are related to certificate store management, there are other kinds of jobs that
ship with the platform and it is possible to create custom jobs. The same job flow works for all job types,
including the completion handler step.

#### RunHandler method

The Completion Handler is a .NET class that implements the `IOrchestratorJobCompleteHandler` interface.
The interface contains a single method with a signature of `bool RunHandler(OrchestratorJobCompleteHandlerContext context)`.
The handler's responsibility is to look at the context object (see below),
do whatever processing is necessary, and return a true or false success status.
Since a single completion handler will typically manage multiple job types, the RunHandler method will usually have some
sort of dispatch logic to figure out what kind of job has completed and call the appropriate logic for the job.

Looking at the sample code, you will see an implementation of the RunHandler method, which relays control to an async
version of the method, which in turn determines the job type and calls a job specific method.

The method that handles inventory jobs demonstrates using data from the context object to make an API call back to the 
Command platform to retrieve more information about the inventory job (in this case the history of previous job runs).
Since the API call is an asynchronous call, the code shows how to transition from the synchronous RunHander interface.

The method that handles management jobs demonstrates having a different code path based on the various operation types
can occur with management jobs.

Finally, the method that handles reenrollment jobs doesn't have any logic but shows handling the third kind of job that
is possible with the WinCert store type.

All of the methods trace out control of flow and it should be clear as to where you could put your own logic.
See below for sample trace output.

The interface assemblies needed by the code are shipped with the Command platform and for developers are made available
as NuGet packages on GitHub's package server (in the Keyfactor Organization). The sample uses the following Keyfactor
specific packages: 
- `Keyfactor.Logging`
- `Keyfactor.Orchestrators.Common`
- `Keyfactor.Platform.IOrchestratorJobCompleteHandler`

#### Context Object

When the completion handler is executed, it is passed a context object that contains information about the job that just completed.
Depending on the job type, various fields in the context object may or may not be present. The sample code traces out the
contents of the context object.

<details><summary>Sample Inventory Context</summary>

```
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 0b4b411a-4b26-4492-a575-a10adb43a08a,
JobType : WinCertInventory,
JobTypeId : 49b3a17d-cada-4ec8-84c6-7719bf5beef3,
OperationType : Unknown,
CertificateId : null,
RequestTimestamp : 5/3/2023 11:18:48 PM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
```
</details>

<details><summary>Sample Management Context</summary>

```
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 426ec1ab-9f02-4011-8df9-72c4a603ca90,
JobType : WinCertManagement,
JobTypeId : 4be14534-55b0-4cd7-9871-536b55b5e973,
OperationType : Add,
CertificateId : 14,
RequestTimestamp : 5/6/2023 12:29:32 AM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
```
</details>

<details><summary>Sample ReEnrollment Context</summary>

```
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 32354394-955e-4142-a968-479daa294128,
JobType : WinCertReenrollment,
JobTypeId : e868b3f8-9b6a-48b1-91c8-683d71d94f61,
OperationType : Unknown,
CertificateId : 15,
RequestTimestamp : 5/6/2023 12:32:46 AM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
```
</details>

The context fields provided are:

| Field | Description |
|-------|-------------|
|AgentId | The GUID that identifies the Orchestrator that ran the job.|
|Username | The account that the Orchestrator used to authorize itself to the Command platform. Displayed as "Identity" on the Orchestrator management screen|
|ClientMachine | The name of the Orchestrator. This is typically a fully qualified domain name, but can be any string that the Orchestrator declared when it registered with the Command Platform|
|JobResult | Result reported by Orchestrator. (Success, Warning, Failure, Unknown) |
|JobId | These will be fixed for reoccurring jobs, such as inventory, and change for one time jobs |
|JobType | String based Job type |
|JobTypeId | GUID for job type. These will be unique per Command instance. These are the values that were configured in the JobTypes Unity configuration|
|OperationType | Some job types have multiple operations. While inventory jobs report "Unknown", management jobs can report "Add" and "Remove" |
|CertificateID | The internal Command Id for the certificate related to the job. Useful for knowing which certificate was added, remove, or enrolled |
|RequestTimestamp | When the job was originally scheduled |
|CurrentRetryCount | Failed jobs are automatically retried (rescheduled) by the Command platform. This will be zero on the first execution of the job and increment for each retry. |
|Client | The context contains an initialized HTTPClient which is appropriate for making calls to the Command API|

#### Sample Trace Outputs


<details><summary>Sample Inventory Handler Trace</summary>

```
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - This handler's favorite animal is: Tiger
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - The context passed is: 
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 0b4b411a-4b26-4492-a575-a10adb43a08a,
JobType : WinCertInventory,
JobTypeId : 49b3a17d-cada-4ec8-84c6-7719bf5beef3,
OperationType : Unknown,
CertificateId : null,
RequestTimestamp : 5/3/2023 11:18:48 PM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Dispatching job completion handler for an Inventory job 0b4b411a-4b26-4492-a575-a10adb43a08a
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Executing the Inventory handler
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Custom logic for Inventory completion handler here
2023-05-05 17:30:00.3527 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Querying Command API with: OrchestratorJobs/JobHistory?pq.queryString=JobID%20-eq%20%220b4b411a-4b26-4492-a575-a10adb43a08a%22
2023-05-05 17:30:00.3683 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Results of JobHistory API: [{"JobHistoryId":2168,"AgentMachine":"COMMAND","JobId":"0b4b411a-4b26-4492-a575-a10adb43a08a","Schedule":{"Interval":{"Minutes":1}},"JobType":"WinCertInventory","OperationStart":"2023-05-06T00:27:00","OperationEnd":"2023-05-06T00:27:00","Message":"","Result":"Success","Status":"Completed","StorePath":"My","ClientMachine":"iistarget.boingy.com"},{"JobHistoryId":2169,"AgentMachine":"COMMAND","JobId":"0b4b411a-4b26-4492-a575-a10adb43a08a","Schedule":{"Interval":{"Minutes":1}},"JobType":"WinCertInventory","OperationStart":"2023-05-06T00:28:00","OperationEnd":"2023-05-06T00:28:00","Message":"","Result":"Success","Status":"Completed","StorePath":"My","ClientMachine":"iistarget.boingy.com"},{"JobHistoryId":2170,"AgentMachine":"COMMAND","JobId":"0b4b411a-4b26-4492-a575-a10adb43a08a","Schedule":{"Interval":{"Minutes":1}},"JobType":"WinCertInventory","OperationStart":"2023-05-06T00:29:00","OperationEnd":"2023-05-06T00:29:00","Message":"","Result":"Success","Status":"Completed","StorePath":"My","ClientMachine":"iistarget.boingy.com"},{"JobHistoryId":2171,"AgentMachine":"COMMAND","JobId":"0b4b411a-4b26-4492-a575-a10adb43a08a","Schedule":{"Interval":{"Minutes":1}},"JobType":"WinCertInventory","OperationStart":"2023-05-06T00:30:00","OperationEnd":null,"Message":"","Result":"Unknown","Status":"InProcess","StorePath":"My","ClientMachine":"iistarget.boingy.com"}]
2023-05-05 17:30:00.3844 E014A91C-90BA-46C8-B185-9F9290993C69 KFSample.SampleJobCompletionHandler [Trace] - Exiting Job Completion Handler for orchestrator [fa406103-d84a-43ea-8bfd-99bc1f064281/COMMAND] and JobType 'WinCertInventory' with status: True

```
</details>

<details><summary>Sample Management Handler Trace</summary>

```
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Entering Job Completion Handler for orchestrator [fa406103-d84a-43ea-8bfd-99bc1f064281/COMMAND] and JobType 'WinCertManagement'
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - This handler's favorite animal is: Tiger
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - The context passed is: 
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 426ec1ab-9f02-4011-8df9-72c4a603ca90,
JobType : WinCertManagement,
JobTypeId : 4be14534-55b0-4cd7-9871-536b55b5e973,
OperationType : Add,
CertificateId : 14,
RequestTimestamp : 5/6/2023 12:29:32 AM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Dispatching job completion handler for a Management job 426ec1ab-9f02-4011-8df9-72c4a603ca90
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Executing the Management handler
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Custom logic for Inventory completion handler here
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Management job process for an Add operation
2023-05-05 17:30:30.0244 F1CF5BD5-3148-47B5-A24D-5A81F21C86CA KFSample.SampleJobCompletionHandler [Trace] - Exiting Job Completion Handler for orchestrator [fa406103-d84a-43ea-8bfd-99bc1f064281/COMMAND] and JobType 'WinCertManagement' with status: True

```
</details>

<details><summary>Sample ReEnrollment Handler Trace</summary>

```
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - Entering Job Completion Handler for orchestrator [fa406103-d84a-43ea-8bfd-99bc1f064281/COMMAND] and JobType 'WinCertReenrollment'
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - This handler's favorite animal is: Tiger
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - The context passed is: 
[
AgentId : fa406103-d84a-43ea-8bfd-99bc1f064281,
Username : BOINGY\CMS_Service,
ClientMachine : COMMAND,
JobResult : Success,
JobId : 32354394-955e-4142-a968-479daa294128,
JobType : WinCertReenrollment,
JobTypeId : e868b3f8-9b6a-48b1-91c8-683d71d94f61,
OperationType : Unknown,
CertificateId : 15,
RequestTimestamp : 5/6/2023 12:32:46 AM,
CurrentRetryCount : 0,
Client : https://command.boingy.com/KeyfactorAPI/
]
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - Dispatching job completion handler for the re-enrollment job 32354394-955e-4142-a968-479daa294128
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - Executing the Reenrollment handler
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - Custom logic for Reenrollment completion handler here
2023-05-05 17:33:31.7903 F81ECB91-496C-4C66-8B66-C2BA4C6CE9F5 KFSample.SampleJobCompletionHandler [Trace] - Exiting Job Completion Handler for orchestrator [fa406103-d84a-43ea-8bfd-99bc1f064281/COMMAND] and JobType 'WinCertReenrollment' with status: True

```
</details>

# Summary
This document has provided a framework necessary to create a Job Completion Handler for a specific job type.
Information was provided for using the Keyfactor APIs to find the correct GUIDs to process specific job types.
The framework also provided examples for handling Inventory, Management and Reenrollment jobs, along with how to access the context properties and a working HTTP Client.
Once the handler has been developed, instructions were provided describing how to add the handler to the Unity container so Keyfactor Command can call the appropriate handler.
For additional information, please refer to the comments in the example source code.


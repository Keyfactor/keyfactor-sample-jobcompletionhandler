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

Use Visual Studio to build the solution file in this repository.

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


# Understanding the Code
The source code does provide additional comments to help understand the code.

Job Completion Handlers can run for the following job types:
1. Discovery
2. Inventory
3. Management
4. Re-enrollment

This example completion handler shows the minimum to get you started. This is a framework for creating other JobCompletionHandlers.

The example application contains the class BaseJobCompletionHandler.cs which implements the IOrchestratorJobCompleteHandler.  The interface Implements a string property for the JobType GUIDs and a Boolean method RunHandler that includes the OrchestratorJobCompleteHandlerContext (context) object.
You will use the context object containing various properties to complete your handler.

The JobTypes property is a string of comma delimited GUIDs identifying which jobs this handler will be executed for. 



Once Keyfactor Command determines the JobType exists, the `RunHander` method will get called, passing the `OrchestratorJobCompletionHandlerContext` object which contains various property information and HTTP client you can use throughout your handler.  
The context object also contains a JobType property that contains the name of the Store Type Capability.  The JobType is a string concatination of the cert store short name and the job capability.  For example, the cert store name of WinCert that performs a reenrollment, the JobType property would contain the string WinCertReenrollment.  Most common job types include:
- Discovery
- Inventory
- Management
- Reenrollment




# Summary
This document has provided us a framework necessary to create a Job Completion Handler for any specific job type.  Information was provided for using the Keyfactor APIs to find the correct GUIDs to process specific job types.  The framework also provided examples for handling Inventory, Management and Reenrollment jobs, along with how to access the context properties and a working HTTP Client.  Once the handler has been developed, instructions were provided describing how to add the handler to the Unity container so Keyfactor Command can call the appropriate handler.  For additional information, please refer to the comments in the example source code.


Left over text


Users can create a completion handler from scratch by performing the following:
- Create a new C# class library solution (.dll) project using the classic .Net Framework 4.7 or later (not .NET Core)
- Add the Keyfactor.Platform.Extensions.IOrchestratorJobCompletionHandler nuget package to the project
- Create a class that implements the IOrchestratorJobCompleteHandler interface.  The contents of the RunHandler function should be the implementation of your job.  You have available to you the OrchestratorJobCompleteHandlerContext object which contains various information about the completed job.

From there you may follow along with this documentation to build your custom handler.  This document describes how to create a custom job completion handler by using the existing template and code you can download from this repo.  


# keyfactor-jobcompletionhandler-base
A base framework for implementing a job completion handler in Keyfactor.

The current handler has been tested with Keyfactor v10.x

# Overview
A Job Completion Hander is an event that gets raised when any job has been completed.  Users can write custom code to perform specific tasks once a job is completed.  Completion handlers get raised whether a job completes successfully or fails.  If there is a valid job registered and configured properly, the custom completion handler logic will be executed.

# Getting Started
Users can create a completion handler from scratch by performing the following:
- Create a new C# class library solution (.dll) project using the classic .Net Framework 4.7 or later (not .NET Core)
- Add the Keyfactor.Platform.Extensions.IOrchestratorJobCompletionHandler nuget package to the project
- Create a class that implements the IOrchestratorJobCompleteHandler interface.  The contents of the RunHandler function should be the implementation of your job.  You have available to you the OrchestratorJobCompleteHandlerContext object which contains various information about the completed job.

From there you may follow along with this documentation to build your custom handler.  This document describes how to create a custom job completion handler by using the existing template and code you can download from this repo.  

## Download Template Code
Using your favorite means, download and bring the code from this repo into Visual Studio to start building your custom Job Completion Handler.

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

## Finding Your GUIDs
Using the Keyfactor API Reference and Utility, scroll down to the *CertificateStoreType* API, and select the GET/CertificateStoreTypes/Name/{name} end point. Enter the short name of the store type you wish inquire about.  Click the `Try it out!` button to execute the API.

Additionally, you can execute the following curl command, replaceing {name} with the short name of your store type.

`curl -X GET --header 'Accept: application/json' --header 'x-keyfactor-api-version: 1' --header 'x-keyfactor-requested-with: APIClient' 'https://{server}/Keyfactor/API/CertificateStoreTypes/Name/{name}'`

Once the response is returned, scroll down to the end of the JSON result.  You should see a list of Job Types and their associated GUIDs.  Below is an example of Job Types - there could be more or less job types depending on what the store type is set up to execute:

![StoreTypeResponse](https://user-images.githubusercontent.com/55611381/230181237-673b9e1e-9d08-4d94-bce7-070d09d9d92a.png)

Once Keyfactor Command determines the JobType exists, the `RunHander` method will get called, passing the `OrchestratorJobCompletionHandlerContext` object which contains various property information and HTTP client you can use throughout your handler.  
The context object also contains a JobType property that contains the name of the Store Type Capability.  The JobType is a string concatination of the cert store short name and the job capability.  For example, the cert store name of WinCert that performs a reenrollment, the JobType property would contain the string WinCertReenrollment.  Most common job types include:
- Discovery
- Inventory
- Management
- Reenrollment

# Registering Job Completion Handlers
To add this handler to KeyFactor:
- Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config
- Add the following new registration inside of <unity><container> along with the other <register ... /> items
```
<register type="IOrchestratorJobCompleteHandler" mapTo="KFSample.SampleJobCompletionHandler, SampleJobCompletionHandler" name="SampleJobCompletionHandler">
    <property name="JobTypes" value="" />            <!-- The value should include a valid GUID for the JobTypes you wish to execute this completion handler for -->
    <property name="FavoriteAnimal" value="Tiger" /> <!-- Sample parameter to pass into the handler. This parameter must be a public property on the class -->
</register>
```

The compiled assembly **SampleJobCompletionHandler.dll** goes in:
- C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin 
    
For this sample only the above DLL needs to be copied to the target system.

In cases where your code may need additional dependent assemblies, make sure to only copy assemblies that are specific to your handler. 
Do not overwrite DLLs that ship with the Command platform. 
You will need to make sure that the handler references the same versions of libraries already in use in the WebAgentServices location.
A compatibility list for library dependencies is in the works.

This Unity registration will require that the web server is restarted, which can be done when safe by running iisreset in a command console. The Agent API server should be checked at this point as errors in the handler registration can prevent other Keyfactor Orchestrators from communicating with the platform.

To be able to see the trace log messages in the sample without having to enable trace level logging for the whole 
Orchestrator API endpoint, find the nlog config file for the Orchestrator API endpoint, usually located at:

`C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config`

and add the following rule inside the `<Rules>` section:

`<logger name="*.SampleJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>`

just before the default logging rule:

 `<logger name="*" minlevel="Info" writeTo="logfile" />`

# Summary
This document has provided us a framework necessary to create a Job Completion Handler for any specific job type.  Information was provided for using the Keyfactor APIs to find the correct GUIDs to process specific job types.  The framework also provided examples for handling Inventory, Management and Reenrollment jobs, along with how to access the context properties and a working HTTP Client.  Once the handler has been developed, instructions were provided describing how to add the handler to the Unity container so Keyfactor Command can call the appropriate handler.  For additional information, please refer to the comments in the example source code.

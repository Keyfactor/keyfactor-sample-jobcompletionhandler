# keyfactor-jobcompletionhandler-base
A base framework for implementing a job completion handler in Keyfactor.

Current main branch works with Keyfactor v10.x

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

This example Orchestrator Job Completion Handler runs for the following jobs:
1. Inventory
2. Management
3. Re-enrollment

The completion handler doesn't do anything. Instead this is a framework for creating other JobCompletionHandlers.

The example application contains the class BaseJobCompletionHandler.cs which implements the IOrchestratorJobCompleteHandler.  The interface Implements a string property for the JobType GUIDs and a Boolean method RunHandler that includes the OrchestratorJobCompleteHandlerContext (context) object.
You will use the context object containing various properties to complete your handler.  See the appendix below for additional details regarding the context object.

The JobTypes property is a string of comma delimited GUIDs identifying which jobs this handler will be executed for.  To get the list of your GUIDs for the jobs you wish to execute will depend on how your environment is hosted.  If you are a Cloud Hosted customer, you will need to reach out to a Keyfactor representative to help you receive the GUIDs.  If you are a self-hosted environment and have access to the database, you can get the GUIDs from the [cms_agents.JobTypes] table in the KFCommand database.

Once Keyfactor Command determines the JobType exists, the `RunHander` method that passes the `OrchestratorJobCompletionHandlerContext` object which contains various property information and HTTP client you can use throughout your handler.  The context object also contains a JobType property that contains the name of the Store Type Capability.  The JobType comes from the Name field from the same [cms_agents.JobTypes] table.  This valus is comprised of the Store Type name and the capability.  For example, a store type name of WinCert has the Inventory, Management and Reenrollment capabilities, the JobType names would be WinCertInventory, WinCertManagement and WinCertReenrollment.  Please be aware that for each capability, the matching GUID must be included when setting up the handler in Unity.

# Registering Job Completion Handlers
To add this to KeyFactor:
- Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config
- Add the following new registration inside of <unity><container> along with the other <register ... /> items
```
<register type="IOrchestratorJobCompleteHandler" mapTo="SampleExtensions.BaseJobCompletionHandler, keyfactor-jobcompletionhandler-base" name="BaseJobCompletionHandler">
    <property name="JobTypes" value="" /> <!-- A comma delimited list of given GUIDs -->
    <property name="KeyfactorAPI" value="https://someurl.kfops.com/KeyfactorAPI" /> <!-- for example Target for the Keyfactor API -->
    <property name="AuthHeader" value="Basic b64encodedusername:password" /> <!-- for example Basic S0VZRkFDVE9SXHNvbWVvbmU6c29tZXBhc3N3b3J -->
</register>
```

The compiled assembly **keyfactor-jobcompletionhandler-base.dll** goes in:
- C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin 
    
Important note: when copying over dependencies for the handler, it is important to not override existing DLLs in the target location. You will need to make sure that the handler references the same versions of libraries already in use in the WebAgentServices location. A compatibility list for library dependencies is in the works.

Adding the following to the KF Nlog config in the <Rules> section will output at trace the registration related information
- Usually located at C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config
```
<logger name="*.BaseJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>
```

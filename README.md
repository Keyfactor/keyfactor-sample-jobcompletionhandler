# keyfactor-jobcompletionhandler-base
A base framework for implementing a job completion handler in Keyfactor.

Current main branch works with Keyfactor v10.x

# Quick description
This Orchestrator Job Completion Handler runs for the following jobs:
1. Re-enrollment

For the re-enrollment jobs, the completion handler doesn't do anything. Instead this is a framework for creating other JobCompletionHandlers

To add this to KeyFactor:
- Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config
- Add the following new registration inside of <unity><container> along with the other <register ... /> items
```
<register type="IOrchestratorJobCompleteHandler" mapTo="SampleExtensions.BaseJobCompletionHandler, keyfactor-jobcompletionhandler-base" name="BaseJobCompletionHandler">
    <property name="KeyfactorAPI" value="https://someurl.kfops.com/KeyfactorAPI" /> <!-- Target for the Keyfactor API -->
    <property name="AuthHeader" value="Basic b64encodedusername:password" /> <!-- for example Basic S0VZRkFDVE9SXHNvbWVvbmU6c29tZXBhc3N3b3J -->
</register>
```

The compiled assembly **keyfactor-jobcompletionhandler-base.dll** goes in:
- C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin 

Adding the following to the KF Nlog config in the <Rules> section will output at trace the registration related information
- Usually located at C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config
```
<logger name="*.BaseJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>
```

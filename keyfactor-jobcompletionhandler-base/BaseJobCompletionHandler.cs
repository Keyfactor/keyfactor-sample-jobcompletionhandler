/*
// Copyright 2023 Keyfactor                                                   
// Licensed under the Apache License, Version 2.0 (the "License"); you may    
// not use this file except in compliance with the License.  You may obtain a 
// copy of the License at http://www.apache.org/licenses/LICENSE-2.0.  Unless 
// required by applicable law or agreed to in writing, software distributed   
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES   
// OR CONDITIONS OF ANY KIND, either express or implied. See the License for  
// the specific language governing permissions and limitations under the       
// License. 
*/

using Keyfactor.Platform.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

using System.Reflection.Emit;
using System.Net.Http;
using System.Diagnostics;

//
// This Sample Orchestrator Job Completion Handler demonstrates how to execute specialized code for the following jobs:
//    1) Inventory
//    2) Management
//    3) Re-enrollment
//
// A discovery job also has the ability to run a completion handler.
//
// To add this handler to KeyFactor:
//
// Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config on the IIS server
//
// and add the following new registration inside of <unity><container> along with the other <register ... /> items

/*
<register type="IOrchestratorJobCompleteHandler" mapTo="SampleExtensions.BaseJobCompletionHandler, keyfactor-jobcompletionhandler-base" name="BaseJobCompletionHandler">
    <property name="JobTypes" value="" />            <!-- The value should include a valid GUID for the JobTypes you wish to execute this completion handler for -->
    <property name="FavoriteAnimal" value="Tiger" /> <!-- Sample parameter that the completion handler class has as a public property -->
</register>
*/
//
// The compiled assembly keyfactor-jobcompletionhandler-base.dll goes in C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin 

// Adding the following to the KF Nlog config in the <Rules> section will output at trace the registration related information
// Usually located at C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config
/*
<logger name="*.BaseJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>
*/
namespace SampleExtensions
{
    public class BaseJobCompletionHandler : IOrchestratorJobCompleteHandler
    {
        #region Logger Setup

        // Setup a logger object so that we can emit log messages to the standard Command logs.

        private ILogger _logger;
        public ILogger Logger
        {
            get
            {
                if (null == _logger)
                {
                    _logger = LogHandler.GetReflectedClassLogger(this);
                }

                return _logger;
            }
        }

        #endregion

        #region Unity Properties

        // JobTypes is a required public property. It will contain a comma separated list of the job type GUIDs that
        // that the handler should be prepared to handle. This value is set in the Unity configuration.
        // Command will only call this handler when jobs of types in this list are complete.
        // This sample doesn't make use of this property.

        public string JobTypes { get; set; }

        // Custom parameters may be passed into the completion handler via the Unity registration in the web.config 
        // file for the Orchestrator API endpoint. When the Unity Dependency Injection loads this class, it will apply
        // values in the configuration to the class public properties.
        //
        // Typically these would be used to configure the behavior of the handler. These might be used to indicate
        // if the logic in the handler should operate in production or test mode; or to configure external API 
        // addresses or credentials needed to talk to external systems.
        //
        // In this sample we will log out the parameter passed in.

        public string FavoriteAnimal { get; set; }

        #endregion

        #region Hard Coded Job Configuration

        // When this handler is called, a context object is included that contains the internal name
        // of the job type that completed. For certificate store job types, this string is the internal
        // certificate store type name (capability name) concatenated with logical job type (Inventory, 
        // Management, Reenrollment, Discovery). For non-store related jobs different job type names may occur.

        // In this sample we are handling Windows Certificate (WinCert) jobs, so we set up some constants 
        // to make the dispatching of handlers for each job easier. The WinCert orchestrator extension doesn't
        // perform discovery jobs, so we don't map it.

        private const string Inventory = "WinCertInventory";
        private const string Management = "WinCertManagement";
        private const string Reenrollment = "WinCertReenrollment";

        #endregion

        #region Handler Entry Point

        // Whenever an orchestrator job completes whose JobType GUID is in the Unity configured list of GUIDs (JobTypes),
        // the RunHandler method is called and passed a context object containing the details of the job that has
        // completed. It is up to the code in the handler to perform whatever custom processing is desired.
        //
        // Note that this method is a synchronous function call. It is not a task and should not be defined
        // as async. Because this sample demonstrates making async calls to the Command API, we need to transition
        // from synchronous to asynchronous - this is done by wrapping our dispatch call in a Task.Run

        public bool RunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Task<bool> RunHandlerResult = Task.Run<bool>(async () => await AsyncRunHandler(context));
            return RunHandlerResult.Result;
        } // RunHandler


        // Async entry point

        private async Task<bool> AsyncRunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false; // Assume failure

            // Make sure we got a context

            if (null == context)
            {
                Logger.LogError($"Error!  The context passed to the Job Handler is null");
                return bResult;
            }

            Logger.LogTrace($"Entering Job Completion Handler for orchestrator = [{context.AgentId}/{context.ClientMachine}] and JobType = {context.JobType}");
            Logger.LogTrace($"This handler's favorite animal is: {FavoriteAnimal}");
            Logger.LogTrace($"The context passed is: \r\n[\r\n{ParseContext(context)}\r\n]");       // This logs the entire context to the log file


            switch (context.JobType)
            {
                // Example of linking to the various job types & executing it.
                // NOTE: The assumption is if we are going to do some REST API calls, so we should be performing async operations
                case Inventory:
                    Logger.LogTrace($"Performing job completion handler for the Inventory job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => executeInventoryHandler(context));
                    break;
                case Management:
                    Logger.LogTrace($"Performing job completion handler for the Management job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => executeManagementHandler(context));
                    break;
                case Reenrollment:
                    Logger.LogTrace($"Performing job completion handler for the re-enrollment job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => executeReenrollmentHandler(context));
                    break;
                default:
                    Logger.LogTrace($"{context.JobType} is not implemented by the completion handler. It needs an implementation defined.");
                    break;
            }
            return bResult;
        } // RunHandler
        #endregion

        #region private methods
        /// <summary>
        /// Call this class to handle the details of running the Inventory handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool executeInventoryHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Executing the Inventory handler and passing control to it for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            try
            {
                // This is where you can create your own custom logic after the Inventory job has been completed.
                Logger.LogTrace("The job completion handler for Inventory code would execute here.");


                // It is a common use case that job completion handlers need to reach back into the Command API to
                // perform various operations. For this purpose, the context object contains an initialized 
                // HTTPClient that points to the Command API on the server that is calling this handler. The client also has
                // the security context of Application Pool with which the Orchestrator endpoint is running as. 
                // Assuming that Windows Authentication is enabled on the endpoint (which is by default), and
                // assuming that the App Pool account has been granted the appropriate API privileges, this allows
                // easy calling of the API without having to manage a set of credentials in code or in the Unity
                // configuration.

                // In this example we retrieve the job history for the job that just completed. We don't do anything
                // with the results, but history could be examined to see if the job we repeatedly failing and then
                // take some corrective action. For this example to work, the App Pool account needs to have been granted
                // the Agent Management Read permission

                if (context.Client != null)
                {
                    // Command API Orchestrator Job History query
                    string query = $@"OrchestratorJobs/JobHistory?pq.queryString=JobID%20-eq%20%22{context.JobId}%22";

                    // Execute the API call with the provided HTTPClient
                    Task<HttpResponseMessage> task = Task.Run<HttpResponseMessage>(async () => await context.Client.GetAsync(query));
                    string result = task.Result.Content.ReadAsStringAsync().Result;

                    // Log out the results - normally some processing would be done on the results
                    Logger.LogTrace($"Results of JobHistory API: {result}");

                    // If custom code completed successfully, change the result flag to True marking the handler as complete and successful.
                    bResult = true;
                }
                else
                {
                    Logger.LogError($"No HttpClient supplied in the context for orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    throw new Exception($"No HttpClient supplied in the context for orchestrator [{context.AgentId}/{context.ClientMachine}]");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Inventory Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Inventory Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;

        } // executeInventoryHandler

        /// <summary>
        /// Call this class to handle the details of running the management handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool executeManagementHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Executing the Management handler and passing control to it for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            try
            {
                // The OperationType property for a management job supports multiple job types.
                // Refer to Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType for the complete list of operation types.

                Logger.LogTrace($"This is where you could create custom for operation type: {context.OperationType}");


                bResult = true;

            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Management Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Management Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;

        } // executeManagementHandler

        /// <summary>
        /// Call the class that handles the details of running the reenrollment handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool executeReenrollmentHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Logger.LogTrace($"Entered ReenrollmentHander handler for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            bool bResult = false;

            try
            {
                Logger.LogTrace($"Custom logic for Reenrollment completion handler here");

                bResult = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Reenrollment Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Reenrollment Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;
        } // executeReenrollmentHandler

        /// <summary>
        /// Convert the context into something printable
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string ParseContext(OrchestratorJobCompleteHandlerContext context)
        {
            string[] pairs = new string[12];
            string result;

            pairs[0] = string.Join(" : ", nameof(context.AgentId), context.AgentId.ToString());
            pairs[1] = string.Join(" : ", nameof(context.Username), context.Username);
            pairs[2] = string.Join(" : ", nameof(context.ClientMachine), context.ClientMachine);
            pairs[3] = string.Join(" : ", nameof(context.JobResult), context.JobResult.ToString());
            pairs[4] = string.Join(" : ", nameof(context.JobId), context.JobId);
            pairs[5] = string.Join(" : ", nameof(context.JobType), context.JobType);
            pairs[6] = string.Join(" : ", nameof(context.JobTypeId), context.JobTypeId.ToString());
            pairs[7] = string.Join(" : ", nameof(context.OperationType), context.OperationType.ToString());
            pairs[8] = string.Join(" : ", nameof(context.CertificateId), context.CertificateId == null ? "null" : context.CertificateId.ToString());
            pairs[9] = string.Join(" : ", nameof(context.RequestTimestamp), context.RequestTimestamp == null ? "null" : context.RequestTimestamp.ToString());
            pairs[10] = string.Join(" : ", nameof(context.CurrentRetryCount), context.CurrentRetryCount.ToString());
            pairs[11] = string.Join(" : ", nameof(context.Client), context.Client.BaseAddress == null ? "null" : context.Client.BaseAddress.ToString());

            result = string.Join(",\r\n", pairs);

            return result;
        } // ParseContext
        #endregion
    }
}

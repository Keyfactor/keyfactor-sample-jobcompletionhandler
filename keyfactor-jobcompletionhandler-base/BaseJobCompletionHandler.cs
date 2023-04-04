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

using SampleExtensions.Handlers.JobCompletion;
using SampleExtensions.Handlers.JobCompletion.Models;
using System.Reflection.Emit;

//
// This Sample Orchestrator Job Completion Handler demonstrates how to execute specialized code for the following jobs:
//    1) Inventory
//    2) Management
//    3) Re-enrollment
//
// For all jobs, the completion handler doesn't do anything.
// Instead a framework
//
// To add this to KeyFactor:
//
// Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config
//
// and add the following new registration inside of <unity><container> along with the other <register ... /> items

/*
<register type="IOrchestratorJobCompleteHandler" mapTo="SampleExtensions.BaseJobCompletionHandler, keyfactor-jobcompletionhandler-base" name="BaseJobCompletionHandler">
    <property name="JobTypes" value="" /> <!-- The value should include a valid GUID for the JobTypes you wish to execute this completion handler for -->
    <property name="KeyfactorAPI" value="https://someurl.kfops.com/KeyfactorAPI" /> <!-- Target for the Keyfactor API -->
    <property name="AuthHeader" value="Basic b64encodedusername:password" /> <!-- for example Basic S0VZRkFDVE9SXHNvbWVvbmU6c29tZXBhc3N3b3J -->
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
        #region LoggerInit
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
        // Properties are used to send and received data to/from KF Command through Unity's Dependency Injection  
        // These are public properties that are used by Unity when the handler is called
        public string KeyfactorAPI { get; set; }
        public string AuthHeader { get; set; } = String.Empty; // Default to an empty string.
        #endregion

        #region Hard Coded Properties
        // These hard coded values are specific to an example Cert Store WinCert
        private const string Inventory = "WinCertInventory";
        private const string Management = "WinCertManagement";
        private const string Reenrollment = "WinCertReenrollment";
        #endregion

        #region Handler Configuration Methods
        /// <summary>
        /// Which jobs should the handler run?  A comma separated list of capabilities as Strings or GUIDs (from web.config - see above for more info)
        /// </summary>
        public string JobTypes { get; set; }

        /// <summary>
        /// Here is the hook for the job types. This should call the appropriate function for the job.
        /// NOTE: This is a synchronous function call.  It is not a Task, so don't define it as async
        /// Instead, run everything else as Async & wrap the call inside a Task.Run....
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool RunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Task<bool> RunHandlerResult = Task.Run<bool>(async () => await AsyncRunHandler(context));
            return RunHandlerResult.Result;
        } // RunHandler
        #endregion

        #region JobCompletionHandler
        /// <summary>
        /// This is the async form of the Run Handler.  In this manner, we can call HttpClient calls Asynchronously
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> AsyncRunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false; // Assume failure
            if (null == context)
            {
                Logger.LogError($"Error!  The context passed to the Job Handler is null");
                return bResult;
            }

            Logger.LogTrace($"Entering Job Completion Handler for orchestrator = [{context.AgentId}/{context.ClientMachine}] and JobType = {context.JobType}");
            Logger.LogTrace($"The context passed is: \r\n[\r\n{ParseContext(context)}\r\n]");
            switch (context.JobType)
            {
                // Example of linking to the various job types & executing it.
                // NOTE: The assumption is if we are going to do some REST API calls, so we should be performing async operations
                case Inventory:
                    Logger.LogTrace($"Performing job completion handler for the Inventory job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => run_InventoryHandler(context));
                    break;
                case Management:
                    Logger.LogTrace($"Performing job completion handler for the Management job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => run_ManagementHandler(context));
                    break;
                case Reenrollment:
                    Logger.LogTrace($"Performing job completion handler for the re-enrollment job on orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    bResult = await Task.Run(() => run_reenrollmenthandler(context));
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
        private bool run_InventoryHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Creating instance of the Inventory handler and passing control to it for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            try
            {
                InventoryHandler handler = new InventoryHandler();
                handler.do_InventoryHandler(context);
            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Discovery Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Discovery Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;

        } // run_DiscoveryHandler

        /// <summary>
        /// Call this class to handle the details of running the management handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool run_ManagementHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Creating instance of the Management handler and passing control to it for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            try
            {
                ManagementHandler handler = new ManagementHandler(context);

                // Management jobs has multiple Operational Types as shown with the switch below:
                // All operation types are listed for illustration purposes only.
                switch (context.OperationType)
                {       
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Unknown:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Inventory:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Add:
                        handler.do_ManagementAddHandler();
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Remove:
                        handler.do_ManagementRemoveHandler();
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Create:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.CreateAdd:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Reenrollment:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Discovery:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.SetPassword:
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.FetchLogs:
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Management Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Management Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;

        } // run_ManagementHandler

        /// <summary>
        /// Call the class that handles the details of running the reenrollment handler
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool run_reenrollmenthandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;
            Logger.LogTrace($"Creating instance of the reenrollment handler and passing control to it for orchestrator [{context.AgentId}/{context.ClientMachine}]");
            ReEnrollmentHandlerModel parameters = new ReEnrollmentHandlerModel()
            {
                KeyfactorAPI = this.KeyfactorAPI,
                AuthHeader = this.AuthHeader,
            };

            try
            {
                ReEnrollmentHandler handler = new ReEnrollmentHandler(parameters);
                handler.do_ReenrollmentHandler(context);
            }
            catch (Exception ex)
            {
                Logger.LogError($"FAILURE in Reenrollment Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"FAILURE in Reenrollment Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
            }

            return bResult;
        } // run_reenrollmenthandler

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
            pairs[11] = string.Join(" : ", nameof(context.Client), context.Client.ToString());

            result = string.Join(",\r\n", pairs);

            return result;
        } // ParseContext
        #endregion
    }
}

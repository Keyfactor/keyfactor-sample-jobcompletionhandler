/*
// Copyright 2025 Keyfactor                                                   
// Licensed under the Apache License, Version 2.0 (the "License"); you may    
// not use this file except in compliance with the License.  You may obtain a 
// copy of the License at http://www.apache.org/licenses/LICENSE-2.0.  Unless 
// required by applicable law or agreed to in writing, software distributed   
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES   
// OR CONDITIONS OF ANY KIND, either express or implied. See the License for  
// the specific language governing permissions and limitations under the       
// License. 
*/

using Keyfactor.Logging;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

// This is a sample implementation of the Keyfactor Command Orchestrator Job Completion Handler. 
// This code can be run after either successful or failed completion of an orchestrator job and
// can be used to implement whatever server side post-processing is desired. Typically handlers
// are used to trigger workflow in systems external to Keyfactor Command.

// This is sample code only and not intended or supported for production use.

// This sample is intended to be used alongside the Windows Certificate "WinCert" extension which
// can be found at: https://github.com/Keyfactor/iis-orchestrator.

// This sample implements handlers for WinCert Inventory, Management, and Reenrollment jobs.
// The Inventory handler demonstrates making an API call back into command.
// This sample doesn't generate any side effects when it runs, other that logging out messages, but can be
// used to demonstrate how custom handler code could be used.

// See the accompanying readme.md for prerequisites and other instructions for installation. 
//
// The TLDR; is that you need a working Keyfactor Command instance that is configured with scheduled
// inventory job on a WinCert store type. This assembly needs to be compiled and registered on the 
// Command instance, and the deployment specific GUIDs for the WinCert store jobs need to be configured
// in the manifest.json file for the extension. A sample manifest.json is included in this repository.
//
// The compiled assembly SampleJobCompletionHandler.dll goes in
// C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\Extensions\JobCompleteHandlers
// The manifest.json and all DLL dependencies should be included with the Completion Handler as well.
// To have the binary dependencies included in build output, the following line should be included in the .csproj file:
// <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
// An example can be seen in the SampleJobCompletionHandler.csproj for this project.
//
// Adding a rule to the NLog configuration which traces output from this code can be helpful. The NLog configuration
// is normally at C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\NLog_Orchestrators.config. The 
// following rule 
/*
<logger name="*.SampleJobCompletionHandler" minlevel="Trace" writeTo="logfile" final="true"/>
*/
// should be added in the rules section just before the default logging rule (<logger name="*" minlevel="Info" writeTo="logfile" />)


namespace KFSample
{
    public class SampleJobCompletionHandler : IOrchestratorJobCompleteHandler
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

        #region Handler Configuration Options and Parameters

        // IMPORTANT: Public properties are no longer set automatically by dependency injection.
        // The Options system needs to be used to pass in and use settings for extensions using a manifest.json.

        // Parameters may be passed into the completion handler via Options in the manifest.json file.
        // The Options are loaded in the constructor. Options might not be specified in the manifest.json, so the
        // constructor should handle the fields appropriately. Options need to be accessed to be applied to
        // public properties defined in this class.

        // Typically these would be used to configure the behavior of the handler or other extension. These might be
        // used to indicate if the logic in the handler should operate in production or test mode; or to configure
        // external API addresses or credentials needed to talk to external systems.

        // JobTypes is a required public property. It will contain a comma separated list of the job type GUIDs that
        // that the handler should be prepared to handle. This value must be set by passed in Options in the manifest.json.
        // Command will only call this handler when jobs of types in this list are complete.
        // This sample doesn't make use of this property directly, but it is used implicitly by the platform.

        public string JobTypes { get; set; }

        // Custom options for configuration settings or other purposes can also be defined.
        // In this sample an option with a string value is passed in and will be logged.

        public string FavoriteAnimal { get; set; }

        private readonly Options _options;
        
        // IMPORTANT: The inferred type of the Options subclass here is critical for loading the IOptions<Options>
        // object correctly. Creating the class Options as a public subclass is the most straightforward way to do this.
        // If defining the Options subclass in a separate file, note that the full type
        // MUST BE: <extension namespace>.<extension type>.Options to load properly.
        public class Options
        {
            public string? JobTypes { get; set; }
            public string? FavoriteAnimal { get; set; }
        }

        public SampleJobCompletionHandler(IOptions<Options> options)
        {
            _options = options.Value;
            if (string.IsNullOrEmpty(_options.JobTypes))
            {
                throw new Exception("JobTypes must be specified in Options in order for this Completion Handler to run.");
            }
            JobTypes = _options.JobTypes;
            FavoriteAnimal = string.IsNullOrEmpty(_options.FavoriteAnimal) ? "Unspecified" : _options.FavoriteAnimal;
        }

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

        // Whenever an orchestrator job completes whose JobType GUID is in the JobTypes public property,
        // the RunHandler method is called and passed a context object containing the details of the job that has
        // completed. It is up to the code in the handler to perform whatever custom processing is desired.
        //
        // Note that the RunHandler method is a synchronous function call. It is not a task and should not be defined
        // as async. Because this sample demonstrates making async calls to the Command API, we need to transition
        // from synchronous to asynchronous - this is done by wrapping our dispatch call in a Task.Run

        public bool RunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Task<bool> RunHandlerResult = Task.Run<bool>(async () => await AsyncRunHandler(context));
            return RunHandlerResult.Result;
        }

        // Async entry point

        private async Task<bool> AsyncRunHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false; // Assume failure

            // Make sure we got a context

            if (null == context)
            {
                Logger.LogError($"A null context object was passed to the job completion handler");
                return false;
            }

            Logger.LogInformation($"Entering Job Completion Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] and JobType '{context.JobType}'");
            Logger.LogInformation($"This handler's favorite animal is: {FavoriteAnimal}");
            Logger.LogInformation($"The context passed is: \r\n[\r\n{ParseContext(context)}\r\n]");
            Logger.LogInformation($"Version of Keyfactor.Logging: {typeof(LogHandler).Assembly.GetName().Version.ToString()}");

            // Depending on the job type, call the appropriate handler. We switch on the job name constants
            // define above.

            switch (context.JobType)
            {
                case Inventory:
                    Logger.LogTrace($"Dispatching job completion handler for an Inventory job {context.JobId}");
                    bResult = await InventoryHandler(context); // Inventory handler makes use of an async HTTPClient, so we await it
                    break;
                case Management:
                    Logger.LogTrace($"Dispatching job completion handler for a Management job {context.JobId}");
                    bResult = ManagementHandler(context); // Management handler is synchronous, we just call it
                    break;
                case Reenrollment:
                    Logger.LogTrace($"Dispatching job completion handler for the re-enrollment job {context.JobId}");
                    bResult = ReenrollmentHandler(context); // Reenrollment handler is synchronous, we just call it
                    break;
                default:
                    Logger.LogTrace($"{context.JobType} is not implemented by the completion handler. No action taken for job {context.JobId}");
                    break;
            }

            Logger.LogTrace($"Exiting Job Completion Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] and JobType '{context.JobType}' with status: {bResult}");

            // Inform Command if the we think the handler was successful.
            // As of Command 10.3, declared failures are logged at a debug level. Backlog has been entered to treat these as warnings.
            return bResult;
        }

        #endregion

        #region Handler Methods

        // Handler for Inventory Jobs
        private async Task<bool> InventoryHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Executing the Inventory handler");

            try
            {
                Logger.LogTrace($"Custom logic for Inventory completion handler here");

                // It is a common use case that job completion handlers need to reach back into the Command API to
                // perform various operations. For this purpose, the context object contains an initialized 
                // HTTPClient that points to the Command API on the server that is calling this handler. The client also has
                // the security context of Application Pool with which the Orchestrator endpoint is running as. 
                // Assuming that Windows Authentication is enabled on the endpoint (which is by default), and
                // assuming that the App Pool account has been granted the appropriate API privileges, this allows
                // easy calling of the API without having to manage a set of credentials in code or in the manifest.json.

                // In this example we retrieve the job history for the job that just completed. We don't do anything
                // with the results, but history could be examined to see if the job we repeatedly failing and then
                // take some corrective action. For this example to work, the App Pool account needs to have been granted
                // the Agent Management Read permission

                if (context.Client != null)
                {
                    // Due to Command issue 45418, the HttpClient object passed to us in the context is not always
                    // in a ready state. Because of this we need to create our own HttpClient and copy the BaseAddress
                    // from the provided one.

                    HttpClient localClient = new HttpClient(
                        new HttpClientHandler()
                        {
                            UseDefaultCredentials = true,
                            PreAuthenticate = true
                        }
                        )
                    { BaseAddress = context.Client.BaseAddress };

                    // Command API Orchestrator Job History query
                    string query = $@"OrchestratorJobs/JobHistory?pq.queryString=JobID%20-eq%20%22{context.JobId}%22";

                    Logger.LogTrace($"Querying Command API with: {query}");

                    HttpResponseMessage response = await localClient.GetAsync(query);

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Could not query Command API: {LogHandler.FlattenException(ex)}");
                        return false;
                    }

                    string result = await response.Content.ReadAsStringAsync();

                    // Log out the results - normally some processing would be done on the results
                    Logger.LogTrace($"Results of JobHistory API: {result}");

                    // Report success
                    bResult = true;
                }
                else
                {
                    // For errors we understand or expect, we should log and return a false status
                    Logger.LogError($"No HttpClient supplied in the context for orchestrator [{context.AgentId}/{context.ClientMachine}]");
                    return false;
                }

            }
            catch (Exception ex)
            {
                // Errors we don't understand can be re-thrown and wrapped in an exception so the stack trace is not lost. Command will log these.
                throw new Exception($"FAILURE in Inventory Handler for orchestrator [{context.AgentId}/{context.ClientMachine}]", ex);
            }

            return bResult;
        }

        // Handler for Inventory Jobs
        private bool ManagementHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Executing the Management handler");

            try
            {
                Logger.LogTrace($"Custom logic for Inventory completion handler here");

                // Management jobs can have different operation types. See Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType
                // Note that the CertStoreOperationType enum contains operation types for all job types, so not all values
                // can occur for inventory jobs.

                switch (context.OperationType)
                {
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Add:
                        Logger.LogTrace($"Management job process for an Add operation");
                        break;
                    case Keyfactor.Orchestrators.Common.Enums.CertStoreOperationType.Remove:
                        Logger.LogTrace($"Management job process for a Remove operation");
                        break;
                    default:
                        Logger.LogTrace($"Management job process for operation type {context.OperationType}");
                        break;
                }

                bResult = true;

            }
            catch (Exception ex)
            {
                // Errors we don't understand can be re-thrown and wrapped in an exception so the stack trace is not lost. Command will log these.
                throw new Exception($"FAILURE in Management Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}", ex);
            }

            return bResult;
        }

        // Handler for Reenrollment Jobs
        private bool ReenrollmentHandler(OrchestratorJobCompleteHandlerContext context)
        {
            bool bResult = false;

            Logger.LogTrace($"Executing the Reenrollment handler");

            try
            {
                Logger.LogTrace($"Custom logic for Reenrollment completion handler here");

                bResult = true;
            }
            catch (Exception ex)
            {
                // Errors we don't understand can be re-thrown and wrapped in an exception so the stack trace is not lost. Command will log these.
                throw new Exception($"FAILURE in Reenrollment Handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}", ex);
            }

            return bResult;
        }

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
        }
        #endregion
    }
}

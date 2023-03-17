/*
// Copyright 2023 Keyfactor                                                   
// Licensed under the Apache License, Version 2.0 (the "License"); you may    
// not use this file except in compliance with the License.  You may obtain a 
// copy of the License at http://www.apache.org/licenses/LICENSE-2.0.  Unless 
// required by applicable law or agreed to in writing, software distributed   
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES   
// OR CONDITIONS OF ANY KIND, either express or implied. See the License for  
// thespecific language governing permissions and limitations under the       
// License. 
*/

using Keyfactor.Platform.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

using SampleExtensions.Handlers.JobCompletion;
using SampleExtensions.Handlers.JobCompletion.Models;

//
// This Orchestrator Job Completion Handler runs for the following jobs:
//    1.) Re-enrollment
//
// For the re-enrollment jobs, the completion handler doesn't do anything.
// Instead a framework
//
// To add this to KeyFactor:
//
// Edit C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config
//
// and add the following new registration inside of <unity><container> along with the other <register ... /> items

/*
<register type="IOrchestratorJobCompleteHandler" mapTo="SampleExtensions.BaseJobCompletionHandler, keyfactor-jobcompletionhandler-base" name="BaseJobCompletionHandler">
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
        public string KeyfactorAPI { get; set; }
        public string AuthHeader { get; set; } = String.Empty; // Default to an empty string.
        #endregion

        #region Hard Coded Properties
        private const string PEM_Reenrollment = "Reenrollment";
        //TODO: For your completion handler, just publish the job types you need.  This is a list of every job type possible
        private const string implementedJobTypes = "C367D107-5D39-4565-9F3B-8AD9F8F28350";
        
        //"3BEE64F4-0EF1-4D65-A862-31CAA4CA2F4E,F672718E-DE09-481A-B21B-906AA0A43023," +
        //                                            "AFB8C78D-1436-4C93-A8E7-0218C2CB6955,56C1E4F9-366C-44B3-8CAE-1B8F39E2026C," +
        //    "9B04E8DE-E2CE-4A50-BD3F-31037D3EE751,E9A34338-C99F-46DA-94D2-4F97DC6943AF,77651281-DFFA-47DD-AF47-267A2DEBD617," +
        //    "1253642F-321B-45B4-A754-44C07DDC3D64,63E394D4-B73B-43D3-8A96-07907A864C74,0CFD99FE-61BF-4A1A-91AA-DFF58B75BFBF," +
        //    "0D893E43-FBE4-4E32-8067-6999E70C6864,B98621F3-D779-40D3-8F09-EECF32D68183,433D98CA-E570-4F7A-8F32-4D31DC19002E," +
        //    "7639C64D-BBF1-402A-8838-1A8EE5E06855,1802DF3C-322B-4E4C-AD59-6E84DA1AA88E,76F99194-C129-4954-B76E-11C80816643E," +
        //    "4AE72D54-7F91-4359-9DA5-E1F633031070,5B9CF048-E95F-4331-A510-0CDFABAC1703,CE2325E3-801C-4576-9B0A-5FDCAF66AA88," +
        //    "B80019D6-DF3D-4A8C-9E60-1977B806F545,74D9F7C5-B2AC-4F21-A72B-29B5ADC90651,67B63010-D738-47C3-87A2-9F289466C881," +
        //    "B614F9E2-C56B-421C-B548-03D42EB32F8A,48743E90-108E-4DC3-B81C-7FB830215D1F,5DFF7BAF-E041-43E6-AC66-DE0CD0A26E4C," +
        //    "78FF1C19-893F-4E5E-90A5-0459F1823778,B7C94AD1-8A33-45EA-98B3-D77CA14E7830,60BD1FD9-5AE3-4E32-A1AC-A66E2A8D2B1E," +
        //    "E98151DE-53AD-4B99-B52A-F37118EC0C5C,A809CE1F-1EEA-4738-A38E-15708C89C981,1D411B36-AE72-433F-9F3F-8593E836A1AF," +
        //    "AA015D10-CFFC-41F7-A9A4-C9615F6F3BDF,BECB11D3-413D-4750-8A3A-B2F9C1CF8C30,0D8CF0C8-56CA-4B8A-B16A-C062018E170D," +
        //    "FD157149-31A1-4489-98AB-0759FAFB8187,8AC4E454-41EA-4ED3-96D0-FDB3FB301E0C,6F9D8CE8-CCFB-4FC7-9FCF-95B21A672416," +
        //    "332ABD4E-CF9A-4449-A550-3C984A2399F9,ECD5758A-2F05-49B1-9373-D9BDE7077F59";
        #endregion

        #region Handler Configuration Methods
        /// <summary>
        /// Which jobs should the handler run?  A comma separated list of capabilities as Strings or GUIDs
        /// </summary>
        public string JobTypes
        {
            get { return implementedJobTypes; }
        }

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
                // Show an example of linking to a PEM Reenrollment job & executing it.
                // NOTE: The assumption is that we are going to do some REST API calls, so we should be performing async operations
                case PEM_Reenrollment:
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

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

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging;
using SampleExtensions.Handlers.JobCompletion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SampleExtensions.Handlers.JobCompletion
{
    internal class InventoryHandler
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

        #region private members

        #endregion

        #region constructors
        public InventoryHandler()
        {

        }
        #endregion

        #region publicMethods
        public async void do_InventoryHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Logger.LogTrace($"Entered function InventoryHandler for orchestrator [{context.AgentId}/{context.ClientMachine}]");

            // Only run on a successful job
            if (OrchestratorJobStatusJobResult.Success != context.JobResult)
            {
                Logger.LogError($"Job {context.JobId} for orchestrator [{context.AgentId}/{context.ClientMachine}] wasn't successful; exiting");
                throw new Exception($"Job {context.JobId} for orchestrator [{context.AgentId}/{context.ClientMachine}] wasn't successful; exiting");
            }

            // We need a HttpClient to move forward, too
            if (null == context.Client)
            {
                Logger.LogError($"No HttpClient supplied in the context for orchestrator [{context.AgentId}/{context.ClientMachine}]");
                throw new Exception($"No HttpClient supplied in the context for orchestrator [{context.AgentId}/{context.ClientMachine}]");
            }

            try
            {
                // Do something after inventory job completed
                await doSomethingForInventory(context);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to finish re-enrollment handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"Failed to finish re-enrollment handler for orchestrator [{context.AgentId}/{context.ClientMachine}]");
            }

            return;
        }
        #endregion

        #region private methods
        private Task doSomethingForInventory(OrchestratorJobCompleteHandlerContext context)
        {
            // This method can be used to add custom code to perform additional non-Keyfactor functions.
            return Task.CompletedTask;
        }
        #endregion
    }
}

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using SampleExtensions.Handlers.JobCompletion.Models;

namespace SampleExtensions.Handlers.JobCompletion
{
    /// <summary>
    /// Implement the ReEnrollmentHandler.
    /// Right now, this doesn't do anything.  However, this provides a framework for implementing a specific completion handler class.
    /// </summary>
    internal class ReEnrollmentHandler
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
        private ReEnrollmentHandlerModel _model;
        #endregion

        #region constructors
        public ReEnrollmentHandler()
        {
            throw new Exception($"FATAL ERROR! You must call the ReEnrollmentHandler with a populated ReEnrollmentHandlerModel");
        }

        public ReEnrollmentHandler(ReEnrollmentHandlerModel model)
        {
            _model = new ReEnrollmentHandlerModel
            {
                KeyfactorAPI = model.KeyfactorAPI,
                AuthHeader = model.AuthHeader,
            };
        }
        #endregion

        #region public methods
        /// <summary>
        /// Davita's re-enrollment handler 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async void do_ReenrollmentHandler(OrchestratorJobCompleteHandlerContext context)
        {
            Logger.LogTrace($"Entered function ReenrollmentHandler for orchestrator [{context.AgentId}/{context.ClientMachine}]");

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
                await doSomethingForReenrollment();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to finish re-enrollment handler for orchestrator [{context.AgentId}/{context.ClientMachine}] {ex.Message}");
                throw new Exception($"Failed to finish re-enrollment handler for orchestrator [{context.AgentId}/{context.ClientMachine}]");
            }

            return;
        } // reenrollment_handler
        #endregion

        #region private methods
        private async Task doSomethingForReenrollment()
        {
            // Do something here
            return;
        }
        #endregion
    }
}

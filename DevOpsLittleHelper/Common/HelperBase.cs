using Microsoft.Azure.WebJobs.Host;
using System;

namespace DevOpsLittleHelper
{
    internal abstract class HelperBase
    {
        private readonly TraceWriter _log;

        public HelperBase(TraceWriter log)
        {
            _log = log;
        }

        protected void Log(string message) => _log.Info(message);

        protected void Error(Exception error) => _log.Error("Exception handled.", error);
    }
}

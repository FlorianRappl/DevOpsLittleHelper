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

        protected void Log(String message) => _log.Info(message);
    }
}

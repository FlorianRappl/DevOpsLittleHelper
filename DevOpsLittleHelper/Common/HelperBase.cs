using Microsoft.Azure.WebJobs.Host;

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
    }
}

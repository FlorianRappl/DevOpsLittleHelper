using Microsoft.Azure.WebJobs.Host;

namespace DevOpsLittleHelper
{
    struct HandlerOptions
    {
        public TraceWriter Log;
        public string AccessToken;
        public string PackageName;
        public string ProjectId;
    }
}

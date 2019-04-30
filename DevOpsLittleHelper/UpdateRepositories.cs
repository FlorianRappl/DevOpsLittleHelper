using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    public static class UpdateRepositories
    {
        [FunctionName("UpdateRepositories")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, TraceWriter log)
        {
            var pat = Environment.GetEnvironmentVariable("DEVOPS_PAT") ?? throw new ArgumentException("Missing environment variable DEVOPS_PAT");
            log.Info("Processing request ...");

            var data = await req.Body.GetRequestData().ConfigureAwait(false);
            var projectId = data.ResourceContainers.Project.Id;
            var packageName = "Microsoft.AspNetCore.All";
            var packageVersion = "2.0.1";
            log.Info($"Received data for project {projectId} ...");

            var helper = new Helper(projectId, pat, log);
            var prIds = await helper.UpdateReferencesAndCreatePullRequests(packageName, packageVersion).ConfigureAwait(false);

            return new OkObjectResult(new
            {
                ids = prIds,
                message = $"Pull Requests successfully created.",
            });
        }

    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    public static class UpdateRepositories
    {
        [FunctionName("UpdateRepositories")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, TraceWriter log)
        {
            var pat = Environment.GetEnvironmentVariable("DEVOPS_PAT") ?? throw new ArgumentException("Missing environment variable DEVOPS_PAT");
            var packageName = req.Query["name"].FirstOrDefault() ?? throw new NotSupportedException("Missing package name");
            log.Info("Processing request ...");

            var data = await req.Body.GetRequestData().ConfigureAwait(false);
            var projectId = data.ResourceContainers.Project.Id;
            log.Info($"Received data for project {projectId}.");

            var nuget = new NugetHelper(pat, log);
            var repository = new RepositoryHelper(projectId, pat, log);

            var packageVersion = await nuget.ReadPackageVersion(packageName).ConfigureAwait(false);
            log.Info($"Received version for {packageName}: {packageVersion}.");

            var prIds = await repository.UpdateReferencesAndCreatePullRequests(packageName, packageVersion).ConfigureAwait(false);
            log.Info($"Pull requests successfully created.");

            return new OkObjectResult(new
            {
                ids = prIds,
                message = $"Pull Requests successfully created.",
            });
        }
    }
}

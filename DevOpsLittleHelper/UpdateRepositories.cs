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
            var packageType = req.Query["type"].FirstOrDefault() ?? "dotnet";
            var projects = (req.Query["projects"].FirstOrDefault() ?? "").Split(',').Select(m => m.Trim());

            var data = await req.Body.GetRequestData().ConfigureAwait(false);
            var reason = data.Resource.Reason;

            if (!Constants.IgnoreValidationBuilds || reason != "validateShelveset")
            {
                log.Info("Processing request ...");
                var projectId = data.ResourceContainers.Project.Id;
                var projectIds = projects.Concat(new[] { projectId }).Distinct().ToArray();
                var repository = new RepositoryHelper(projectIds, pat, log);
                var handler = PackageHandlerFactory.Create(packageType, new HandlerOptions
                {
                    AccessToken = pat,
                    Log = log,
                    PackageName = packageName,
                    ProjectIds = projectIds,
                });
                log.Info($"Received data for projects '{string.Join("', '", projectIds)}'.");

                var packageVersion = await handler.GetVersion().ConfigureAwait(false);
                log.Info($"Received version for {packageName}: {packageVersion}.");

                var prIds = await repository.UpdateReferencesAndCreatePullRequests(handler, packageVersion).ConfigureAwait(false);
                log.Info($"Pull requests successfully created.");

                return new OkObjectResult(new
                {
                    ids = prIds,
                    message = $"Pull Requests successfully created.",
                });
            }

            return new OkObjectResult(new
            {
                message = $"Ignored due to Pull Request validation build.",
            });
        }
    }
}

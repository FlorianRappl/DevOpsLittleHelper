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

            //var name = req.Query["name"].FirstOrDefault();
            //var requestBody = new StreamReader(req.Body).ReadToEnd();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            var helper = new Helper(pat);
            var prId = await helper.UpdateReferenceAndCreatePullRequest(log).ConfigureAwait(false);

            return new OkObjectResult(new
            {
                id = prId,
                message = $"Pull Request #{prId} created.",
            });
        }
    }
}

using Microsoft.Azure.WebJobs.Host;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class Helper
    {
        const String collectionUri = "https://florianrappl.visualstudio.com/";
        const String projectName = "MunichMeetup";
        const String repoName = "PublicWeb";
        const String baseBranchName = "master";
        const String newBranchName = "feature/auto-ref-update";

        private readonly GitHttpClient _gitClient;

        public Helper(String pat)
        {
            var creds = new VssBasicCredential(String.Empty, pat);

            // Connect to Azure DevOps Services
            var connection = new VssConnection(new Uri(collectionUri), creds);

            // Get a GitHttpClient to talk to the Git endpoints
            _gitClient = connection.GetClient<GitHttpClient>();
        }

        public async Task<Int32> UpdateReferenceAndCreatePullRequest(TraceWriter log)
        {
            var repo = await _gitClient.GetRepositoryAsync(projectName, repoName).ConfigureAwait(false);
            log.Info($"Received info about repo ${repoName}.");
            var commits = await _gitClient.GetCommitsAsync(repo.Id, new GitQueryCommitsCriteria
            {
                ItemVersion = new GitVersionDescriptor
                {
                    Version = baseBranchName,
                    VersionType = GitVersionType.Branch,
                },
            }, top: 1).ConfigureAwait(false);
            var lastCommit = commits.FirstOrDefault()?.CommitId;
            log.Info($"Received info about last commits (expected 1, got {commits.Count}).");
            var path = "SmartHotel360.PublicWeb/SmartHotel360.PublicWeb.csproj";
            var item = await _gitClient.GetItemContentAsync(repo.Id, path, includeContent: true).ConfigureAwait(false);
            var oldContent = await GetContent(item).ConfigureAwait(false);
            var newContent = oldContent.Replace(
                $"<PackageReference Include=\"Microsoft.AspNetCore.All\" Version=\"2.0.0\" />",
                $"<PackageReference Include=\"Microsoft.AspNetCore.All\" Version=\"2.0.1\" />");
            log.Info($"Item content of {path} received and changed.");
            var push = CreatePush(lastCommit, path, newContent);
            await _gitClient.CreatePushAsync(push, repo.Id).ConfigureAwait(false);
            log.Info($"Push for {repo.Id} at {newBranchName} created.");
            var pr = CreatePullRequest();
            var result = await _gitClient.CreatePullRequestAsync(pr, repo.Id).ConfigureAwait(false);
            log.Info($"Pull request for {repo.Id} to {baseBranchName} created.");
            return result.PullRequestId;
        }

        private GitPullRequest CreatePullRequest() => new GitPullRequest
        {
            Title = "Automatic Reference Update",
            Description = "Updated the reference / automatic job.",
            TargetRefName = GetRefName(baseBranchName),
            SourceRefName = GetRefName(newBranchName),
        };

        private static GitPush CreatePush(String commitId, String path, String content) => new GitPush
        {
            RefUpdates = new List<GitRefUpdate>
            {
                new GitRefUpdate
                {
                    Name = GetRefName(newBranchName),
                    OldObjectId = commitId,
                },
            },
            Commits = new List<GitCommitRef>
            {
                new GitCommitRef
                {
                    Comment = "Automatic reference update",
                    Changes = new List<GitChange>
                    {
                        new GitChange
                        {
                            ChangeType = VersionControlChangeType.Edit,
                            Item = new GitItem
                            {
                                Path = path,
                            },
                            NewContent = new ItemContent
                            {
                                Content = content,
                                ContentType = ItemContentType.RawText,
                            },
                        }
                    },
                }
            },
        };

        private static String GetRefName(String branchName) => $"refs/heads/{branchName}";

        private static async Task<String> GetContent(Stream item)
        {
            using (var ms = new MemoryStream())
            {
                await item.CopyToAsync(ms).ConfigureAwait(false);
                var raw = ms.ToArray();
                return Encoding.UTF8.GetString(raw);
            }
        }
    }
}

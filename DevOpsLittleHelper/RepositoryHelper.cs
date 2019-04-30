using Microsoft.Azure.WebJobs.Host;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class RepositoryHelper : HelperBase
    {
        private static readonly Uri collectionUri = new Uri(Constants.VsoApiRoot);

        private readonly String _projectId;
        private readonly GitHttpClient _gitClient;

        public RepositoryHelper(String projectId, String pat, TraceWriter log)
            : base(log)
        {
            _projectId = projectId;
            _gitClient = CreateGitClient(pat);
        }

        public async Task<List<Int32>> UpdateReferencesAndCreatePullRequests(String packageName, String packageVersion)
        {
            var results = new List<Int32>();
            var allRepositories = await _gitClient.GetRepositoriesAsync(_projectId).ConfigureAwait(false);
            Log($"Received repository list: {String.Join(", ", allRepositories.Select(m => m.Name))}.");

            foreach (var repo in allRepositories)
            {
                var branch = repo.DefaultBranch.Replace("refs/heads/", String.Empty);
                var pr = await UpdateReferencesAndCreatePullRequest(repo.Name, branch, packageName, packageVersion).ConfigureAwait(false);

                if (pr.HasValue)
                {
                    results.Add(pr.Value);
                }
            }

            return results;
        }

        public async Task<Int32?> UpdateReferencesAndCreatePullRequest(String repoName, String baseBranchName, String packageName, String packageVersion)
        {
            var repo = await _gitClient.GetRepositoryAsync(_projectId, repoName).ConfigureAwait(false);
            Log($"Received info about repo {repoName}.");

            var versionRef = GetVersionRef(baseBranchName);
            var baseCommitInfo = GetBaseCommits(versionRef);
            var commits = await _gitClient.GetCommitsAsync(repo.Id, baseCommitInfo, top: 1).ConfigureAwait(false);
            var lastCommit = commits.FirstOrDefault()?.CommitId;
            Log($"Received info about last commits (expected 1, got {commits.Count}).");

            var items = await _gitClient.GetItemsAsync(_projectId, repo.Id, versionDescriptor: versionRef, recursionLevel: VersionControlRecursionType.Full).ConfigureAwait(false);
            var changes = await GetChanges(repo.Id, packageName, packageVersion, versionRef, items).ConfigureAwait(false);
            return await CreatePullRequestIfChanged(repo.Id, changes, lastCommit, baseBranchName).ConfigureAwait(false);
        }

        private async Task<Int32?> CreatePullRequestIfChanged(Guid repoId, List<GitChange> changes, String lastCommit, String baseBranchName)
        {
            if (changes.Count > 0)
            {
                var push = CreatePush(lastCommit, changes);
                await _gitClient.CreatePushAsync(push, repoId).ConfigureAwait(false);
                Log($"Push for {repoId} at {Constants.NewBranchName} created.");

                var pr = CreatePullRequest(baseBranchName);
                var result = await _gitClient.CreatePullRequestAsync(pr, repoId).ConfigureAwait(false);
                Log($"Pull request for {repoId} to {baseBranchName} created.");

                return result.PullRequestId;
            }

            return null;
        }

        private async Task<List<GitChange>> GetChanges(Guid repoId, String packageName, String packageVersion, GitVersionDescriptor versionRef, IEnumerable<GitItem> items)
        {
            var changes = new List<GitChange>();

            foreach (var item in items)
            {
                if (item.Path.EndsWith(".csproj"))
                {
                    var itemRef = await _gitClient.GetItemContentAsync(repoId, item.Path, includeContent: true, versionDescriptor: versionRef).ConfigureAwait(false);
                    var oldContent = await itemRef.GetContent().ConfigureAwait(false);
                    var newContent = ReplaceInContent(oldContent, packageName, packageVersion);

                    if (!String.Equals(oldContent, newContent))
                    {
                        changes.Add(CreateChange(item.Path, newContent));
                        Log($"Item content of {item.Path} received and changed.");
                    }
                }
            }

            return changes;
        }

        private static String ReplaceInContent(String oldContent, String packageName, String packageVersion)
        {
            var start = $"<PackageReference Include=\"{packageName}\" Version=\"";
            var index = oldContent.IndexOf(start);

            if (index != -1)
            {
                var end = index + start.Length;
                var head = oldContent.Substring(0, end);
                var tail = oldContent.Substring(oldContent.IndexOf('"', end));
                return $"{head}{packageVersion}{tail}";
            }

            return oldContent;
        }


        private static GitVersionDescriptor GetVersionRef(String baseBranchName) => new GitVersionDescriptor
        {
            Version = baseBranchName,
            VersionType = GitVersionType.Branch,
        };

        private static GitQueryCommitsCriteria GetBaseCommits(GitVersionDescriptor itemVersion) => new GitQueryCommitsCriteria
        {
            ItemVersion = itemVersion,
        };

        private static GitPullRequest CreatePullRequest(String baseBranchName) => new GitPullRequest
        {
            Title = Constants.NewPrTitle,
            Description = Constants.NewPrDescription,
            TargetRefName = GetRefName(baseBranchName),
            SourceRefName = GetRefName(Constants.NewBranchName),
        };

        private static GitChange CreateChange(String path, String content) => new GitChange
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
        };

        private static GitPush CreatePush(String commitId, IEnumerable<GitChange> changes) => new GitPush
        {
            RefUpdates = new List<GitRefUpdate>
            {
                new GitRefUpdate
                {
                    Name = GetRefName(Constants.NewBranchName),
                    OldObjectId = commitId,
                },
            },
            Commits = new List<GitCommitRef>
            {
                new GitCommitRef
                {
                    Comment = Constants.NewCommitMessage,
                    Changes = new List<GitChange>(changes),
                },
            },
        };

        private static String GetRefName(String branchName) => $"refs/heads/{branchName}";

        private static GitHttpClient CreateGitClient(String pat)
        {
            var creds = new VssBasicCredential(String.Empty, pat);

            // Connect to Azure DevOps Services
            var connection = new VssConnection(collectionUri, creds);

            // Get a GitHttpClient to talk to the Git endpoints
            return connection.GetClient<GitHttpClient>();
        }
    }
}

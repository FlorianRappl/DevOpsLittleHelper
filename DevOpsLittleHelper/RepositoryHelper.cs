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
                var branchName = GetBranchName(repo.DefaultBranch);
                var pr = await UpdateReferencesAndCreatePullRequest(repo.Name, branchName, packageName, packageVersion).ConfigureAwait(false);

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

            var branches = await _gitClient.GetBranchesAsync(repo.Id).ConfigureAwait(false);
            var targetBranchName = GetTargetBranchName(packageName, packageVersion);

            var branch = 
                branches.FirstOrDefault(m => String.Equals(m.Name, targetBranchName, StringComparison.InvariantCultureIgnoreCase)) ??
                branches.FirstOrDefault(m => String.Equals(m.Name, baseBranchName, StringComparison.InvariantCultureIgnoreCase)) ??
                branches.First();

            var lastCommit = branch.Commit.CommitId;
            var versionRef = GetVersionRef(branch);
            Log($"Received info about branches ({String.Join(", ", branches.Select(m => m.Name))}). Selected '{branch.Name}'.");

            var items = await _gitClient.GetItemsAsync(_projectId, repo.Id, versionDescriptor: versionRef, recursionLevel: VersionControlRecursionType.Full).ConfigureAwait(false);
            var changes = await GetChanges(repo.Id, packageName, packageVersion, versionRef, items).ConfigureAwait(false);
            return await CreatePullRequestIfChanged(repo.Id, changes, lastCommit, branch.Name, targetBranchName, packageName, packageVersion).ConfigureAwait(false);
        }

        private async Task<Int32?> CreatePullRequestIfChanged(Guid repoId, List<GitChange> changes, String lastCommit, String baseBranchName, String targetBranchName, String packageName, String packageVersion)
        {
            if (changes.Count > 0)
            {
                var title = GetTitle(packageName, packageVersion);
                var description = GetDescription(packageName, packageVersion);
                var commitMessage = GetCommitMessage(packageName, packageVersion);
                var push = CreatePush(lastCommit, changes, targetBranchName, commitMessage);
                await _gitClient.CreatePushAsync(push, repoId).ConfigureAwait(false);
                Log($"Push for {repoId} at {targetBranchName} created.");

                if (!String.Equals(baseBranchName, targetBranchName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var pr = CreatePullRequest(baseBranchName, targetBranchName, title, description);
                    var result = await _gitClient.CreatePullRequestAsync(pr, repoId).ConfigureAwait(false);
                    Log($"Pull request for {repoId} to {baseBranchName} created.");
                    return result.PullRequestId;
                }

                Log($"Skipping pull request since {baseBranchName} was the target.");
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


        private static GitVersionDescriptor GetVersionRef(GitBranchStats branch) => new GitVersionDescriptor
        {
            Version = branch.Name,
            VersionType = GitVersionType.Branch,
        };

        private static GitQueryCommitsCriteria GetBaseCommits(GitVersionDescriptor itemVersion) => new GitQueryCommitsCriteria
        {
            ItemVersion = itemVersion,
        };

        private static GitPullRequest CreatePullRequest(String baseBranchName, String targetBranchName, String title, String description) => new GitPullRequest
        {
            Title = title,
            Description = description,
            TargetRefName = GetRefName(baseBranchName),
            SourceRefName = GetRefName(targetBranchName),
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

        private static GitPush CreatePush(String commitId, IEnumerable<GitChange> changes, String branchName, String commitMessage) => new GitPush
        {
            RefUpdates = new List<GitRefUpdate>
            {
                new GitRefUpdate
                {
                    Name = GetRefName(branchName),
                    OldObjectId = commitId,
                },
            },
            Commits = new List<GitCommitRef>
            {
                new GitCommitRef
                {
                    Comment = commitMessage,
                    Changes = new List<GitChange>(changes),
                },
            },
        };

        private static String GetCommitMessage(String packageName, String packageVersion) =>
            MakeString(Constants.NewCommitMessage, packageName, packageVersion);

        private static String GetDescription(String packageName, String packageVersion) => 
            MakeString(Constants.NewPrDescription, packageName, packageVersion);

        private static String GetTitle(String packageName, String packageVersion) =>
            MakeString(Constants.NewPrTitle, packageName, packageVersion);

        private static String GetTargetBranchName(String packageName, String packageVersion) =>
            MakeString(Constants.NewBranchName, packageName, packageVersion);

        private static String MakeString(String template, String packageName, String packageVersion) => template
            .Replace("{packageName}", packageName)
            .Replace("{packageVersion}", packageVersion)
            .Replace("{appVersion}", Constants.AppVersion)
            .Replace("{suffix}", $"{packageName}-{packageVersion}".Replace('.', '-').ToLower());

        private static String GetRefName(String branchName) => $"refs/heads/{branchName}";

        private static String GetBranchName(String refName) => refName.Replace("refs/heads/", String.Empty);

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

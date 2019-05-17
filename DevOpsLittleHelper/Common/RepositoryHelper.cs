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

        private readonly string _projectId;
        private readonly GitHttpClient _gitClient;

        public RepositoryHelper(string projectId, string pat, TraceWriter log)
            : base(log)
        {
            _projectId = projectId;
            _gitClient = CreateGitClient(pat);
        }

        public async Task<List<int>> UpdateReferencesAndCreatePullRequests(IPackageHandler handler, string packageVersion)
        {
            var results = new List<int>();
            var allRepositories = await _gitClient.GetRepositoriesAsync(_projectId).ConfigureAwait(false);
            Log($"Received repository list: {string.Join(", ", allRepositories.Select(m => m.Name))}.");

            foreach (var repo in allRepositories)
            {
                // New repositories have no branches yet -> no default branch
                if (repo.DefaultBranch != null)
                {
                    var branchName = GetBranchName(repo.DefaultBranch);
                    var pr = await UpdateReferencesAndCreatePullRequest(repo.Name, branchName, handler, packageVersion).ConfigureAwait(false);

                    if (pr.HasValue)
                    {
                        results.Add(pr.Value);
                    }
                }
            }

            return results;
        }

        public async Task<int?> UpdateReferencesAndCreatePullRequest(string repoName, string baseBranchName, IPackageHandler handler, string packageVersion)
        {
            var replacer = new TemplateReplacer(handler.Name, packageVersion);
            var repo = await _gitClient.GetRepositoryAsync(_projectId, repoName).ConfigureAwait(false);
            Log($"Received info about repo {repoName}.");

            var branches = await _gitClient.GetBranchesAsync(repo.Id).ConfigureAwait(false);
            var targetBranchName = GetTargetBranchName(replacer);

            var branch = 
                branches.FirstOrDefault(m => string.Equals(m.Name, targetBranchName, StringComparison.InvariantCultureIgnoreCase)) ??
                branches.FirstOrDefault(m => string.Equals(m.Name, baseBranchName, StringComparison.InvariantCultureIgnoreCase)) ??
                branches.First();

            var lastCommit = branch.Commit.CommitId;
            var versionRef = GetVersionRef(branch);
            Log($"Received info about branches ({string.Join(", ", branches.Select(m => m.Name))}). Selected '{branch.Name}'.");

            var items = await _gitClient.GetItemsAsync(_projectId, repo.Id, versionDescriptor: versionRef, recursionLevel: VersionControlRecursionType.Full).ConfigureAwait(false);
            var changes = await GetChanges(repo.Id, handler, packageVersion, versionRef, items).ConfigureAwait(false);
            return await CreatePullRequestIfChanged(repo.Id, changes, lastCommit, branch.Name, targetBranchName, replacer).ConfigureAwait(false);
        }

        private async Task<int?> CreatePullRequestIfChanged(Guid repoId, List<GitChange> changes, string lastCommit, string baseBranchName, string targetBranchName, ITemplateReplacer replacer)
        {
            if (changes.Count > 0)
            {
                var title = GetTitle(replacer);
                var description = GetDescription(replacer);
                var commitMessage = GetCommitMessage(replacer);
                var push = CreatePush(lastCommit, changes, targetBranchName, commitMessage);
                await _gitClient.CreatePushAsync(push, repoId).ConfigureAwait(false);
                Log($"Push for {repoId} at {targetBranchName} created.");

                if (!string.Equals(baseBranchName, targetBranchName, StringComparison.InvariantCultureIgnoreCase))
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

        private async Task<List<GitChange>> GetChanges(Guid repoId, IPackageHandler handler, string packageVersion, GitVersionDescriptor versionRef, IEnumerable<GitItem> items)
        {
            var changes = new List<GitChange>();

            foreach (var item in items)
            {
                var shouldUpdate = await handler.ShouldUpdate(item.Path).ConfigureAwait(false);

                if (shouldUpdate)
                {
                    var itemRef = await _gitClient.GetItemContentAsync(repoId, item.Path, includeContent: true, versionDescriptor: versionRef).ConfigureAwait(false);
                    var oldContent = await itemRef.GetContent().ConfigureAwait(false);
                    var newContent = await handler.Update(oldContent, packageVersion).ConfigureAwait(false);

                    if (!string.Equals(oldContent, newContent))
                    {
                        changes.Add(CreateChange(item.Path, newContent));
                        Log($"Item content of {item.Path} received and changed.");
                    }
                }
            }

            return changes;
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

        private static GitPullRequest CreatePullRequest(string baseBranchName, string targetBranchName, string title, string description) => new GitPullRequest
        {
            Title = title,
            Description = description,
            TargetRefName = GetRefName(baseBranchName),
            SourceRefName = GetRefName(targetBranchName),
        };

        private static GitChange CreateChange(string path, string content) => new GitChange
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

        private static GitPush CreatePush(string commitId, IEnumerable<GitChange> changes, string branchName, string commitMessage) => new GitPush
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

        private static string GetCommitMessage(ITemplateReplacer replacer) =>
            replacer.MakeString(Constants.NewCommitMessage);

        private static string GetDescription(ITemplateReplacer replacer) => 
            replacer.MakeString(Constants.NewPrDescription);

        private static string GetTitle(ITemplateReplacer replacer) =>
            replacer.MakeString(Constants.NewPrTitle);

        private static string GetTargetBranchName(ITemplateReplacer replacer) =>
            replacer.MakeString(Constants.NewBranchName);

        private static string GetRefName(string branchName) => $"refs/heads/{branchName}";

        private static string GetBranchName(string refName) => refName.Replace("refs/heads/", string.Empty);

        private static GitHttpClient CreateGitClient(string pat)
        {
            var creds = new VssBasicCredential(string.Empty, pat);

            // Connect to Azure DevOps Services
            var connection = new VssConnection(collectionUri, creds);

            // Get a GitHttpClient to talk to the Git endpoints
            return connection.GetClient<GitHttpClient>();
        }
    }
}

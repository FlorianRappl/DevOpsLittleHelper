using System;

namespace DevOpsLittleHelper
{
    internal class Constants
    {
        public static readonly string VsoGroup = Environment.GetEnvironmentVariable("DEVOPS_ORGA") ?? "your-name-here";

        public static readonly string VsoApiRoot = $"https://{VsoGroup}.visualstudio.com";

        public static readonly string NpmApiRoot = $"https://{VsoGroup}.pkgs.visualstudio.com/_packaging/NPM-Feed@Local/npm/registry";

        public static readonly string NuGetApiRoot = $"https://{VsoGroup}.pkgs.visualstudio.com/_packaging/NuGet-Feed@Local/nuget";

        public static readonly string NewBranchName = Environment.GetEnvironmentVariable("DEVOPS_NEW_BRANCH") ?? "feature/auto-ref-update";

        public static readonly string NewPrTitle = Environment.GetEnvironmentVariable("DEVOPS_PR_TITLE") ?? "Automatic Reference Update ({packageName} v{packageVersion})";

        public static readonly string NewPrDescription = Environment.GetEnvironmentVariable("DEVOPS_PR_DESC") ?? "Updated the reference / automatic job.\n\nPowered by Azure DevOps Little Helper v{appVersion}.";

        public static readonly string NewCommitMessage = Environment.GetEnvironmentVariable("DEVOPS_COMMIT_MSG") ?? "Automatic reference update";

        public static readonly string AppVersion = "0.3.0";
    }
}

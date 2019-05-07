using System;

namespace DevOpsLittleHelper
{
    internal class Constants
    {
        public static readonly String VsoGroup = Environment.GetEnvironmentVariable("DEVOPS_ORGA") ?? "your-name-here";

        public static readonly String VsoApiRoot = $"https://{VsoGroup}.visualstudio.com";

        public static readonly String NuGetApiRoot = $"https://{VsoGroup}.pkgs.visualstudio.com/_packaging/NuGet-Feed@Local/nuget";

        public static readonly String NewBranchName = Environment.GetEnvironmentVariable("DEVOPS_NEW_BRANCH") ?? "feature/auto-ref-update";

        public static readonly String NewPrTitle = Environment.GetEnvironmentVariable("DEVOPS_PR_TITLE") ?? "Automatic Reference Update ({packageName} v{packageVersion})";

        public static readonly String NewPrDescription = Environment.GetEnvironmentVariable("DEVOPS_PR_DESC") ?? "Updated the reference / automatic job.\n\nPowered by Azure DevOps Little Helper v{appVersion}.";

        public static readonly String NewCommitMessage = Environment.GetEnvironmentVariable("DEVOPS_COMMIT_MSG") ?? "Automatic reference update";

        public static readonly String AppVersion = "0.2.0";
    }
}

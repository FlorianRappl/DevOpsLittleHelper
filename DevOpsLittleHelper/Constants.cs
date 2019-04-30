using System;

namespace DevOpsLittleHelper
{
    internal class Constants
    {
        public static readonly String VsoGroup = Environment.GetEnvironmentVariable("DEVOPS_ORGA") ?? "your-name-here";

        public static readonly String VsoApiRoot = $"https://{VsoGroup}.visualstudio.com";

        public static readonly String NuGetApiRoot = $"https://{VsoGroup}.pkgs.visualstudio.com/_packaging/NuGet-Feed@Local/nuget";

        public static readonly String NewBranchName = "feature/auto-ref-update";

        public static readonly String NewPrTitle = "Automatic Reference Update";

        public static readonly String NewPrDescription = "Updated the reference / automatic job.";

        public static readonly String NewCommitMessage = "Automatic reference update";
    }
}

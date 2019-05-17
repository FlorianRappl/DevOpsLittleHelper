using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class DotnetPackageHandler : IPackageHandler
    {
        private readonly HandlerOptions _options;

        public DotnetPackageHandler(HandlerOptions options)
        {
            _options = options;
        }

        public string Name => _options.PackageName;

        public Task<string> GetVersion()
        {
            var nuget = new NugetHelper(_options.AccessToken, _options.Log);
            return nuget.ReadPackageVersion(_options.PackageName);
        }

        public Task<bool> ShouldUpdate(string path) => Task.FromResult(path.EndsWith(".csproj"));

        public Task<string> Update(string content, string version) =>
            Task.FromResult(ReplaceInContent(content, Name, version));

        private static string ReplaceInContent(string oldContent, string packageName, string packageVersion) =>
            oldContent.ReplaceFromStartToQuote($"<PackageReference Include=\"{packageName}\" Version=\"", packageVersion);
    }
}

using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class NodejsPackageHandler : IPackageHandler
    {
        private readonly HandlerOptions _options;

        public NodejsPackageHandler(HandlerOptions options)
        {
            _options = options;
        }

        public string Name => _options.PackageName;

        public Task<string> GetVersion()
        {
            var npm = new NpmHelper(_options.AccessToken, _options.Log);
            return npm.ReadPackageVersion(_options.PackageName);
        }

        public Task<bool> ShouldUpdate(string path) => Task.FromResult(path.EndsWith("/package.json"));

        public Task<string> Update(string content, string version) =>
            Task.FromResult(ReplaceInContent(content, Name, version));

        private static string ReplaceInContent(string oldContent, string packageName, string packageVersion) =>
            oldContent.ReplaceFromStartToQuote($"\"{packageName}\": \"", packageVersion);
    }
}

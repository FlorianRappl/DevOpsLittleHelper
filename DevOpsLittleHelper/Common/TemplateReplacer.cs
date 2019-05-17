namespace DevOpsLittleHelper
{
    internal class TemplateReplacer : ITemplateReplacer
    {
        private readonly string _packageName;
        private readonly string _packageVersion;

        public TemplateReplacer(string packageName, string packageVersion)
        {
            _packageName = packageName;
            _packageVersion = packageVersion;
        }

        public string MakeString(string template) => template
            .Replace("{packageName}", _packageName)
            .Replace("{packageVersion}", _packageVersion)
            .Replace("{appVersion}", Constants.AppVersion)
            .Replace("{suffix}", $"{_packageName}-{_packageVersion}".Replace('.', '-').ToLower());
    }
}

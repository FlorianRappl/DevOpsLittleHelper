using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal interface IPackageHandler
    {
        string Name { get; }

        Task<string> GetVersion();

        Task<bool> ShouldUpdate(string path);

        Task<string> Update(string content, string version);
    }
}

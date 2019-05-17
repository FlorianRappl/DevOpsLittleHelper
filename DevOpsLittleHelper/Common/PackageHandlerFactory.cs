using System;
using System.Collections.Generic;

namespace DevOpsLittleHelper
{
    internal static class PackageHandlerFactory
    {
        private static readonly Dictionary<string, Func<HandlerOptions, IPackageHandler>> _handlers = new Dictionary<string, Func<HandlerOptions, IPackageHandler>>(StringComparer.OrdinalIgnoreCase)
        {
            { "dotnet", options => new DotnetPackageHandler(options) },
            { "nodejs", options => new NodejsPackageHandler(options) },
        };

        public static IPackageHandler Create(string packageType, HandlerOptions options)
        {
            if (_handlers.TryGetValue(packageType, out var creator))
            {
                return creator.Invoke(options);
            }

            throw new NotSupportedException("The provided package type is not supported");
        }
    }
}

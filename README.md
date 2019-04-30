# DevOps Little Helper

This project is the companion for the [serverless challenge article on CodeProject](https://www.codeproject.com/Articles/3941198/Serverless-DevOps-Little-Helper#azure-devops-subscription-setup).

> This solution is designed for Azure Functions to give some value to Azure DevOps solutions.

## Motivation

I wanted to write a simple utility to automatically (propose an) update (of) references with respect to our internally developed / used libraries.

:zap: **Idea**

Whenever a new version of one of these libraries is published a webhook is trigged (Azure DevOps subscription). The triggered functionality inspects all available repositories and updates the references if found / outdated. The update is performed in form of a pull request.

:dollar: **Value Proposition**

We are notified in case of a new pull request and can accept the update or deny it due to whatever reasons (incompatibilities / feature freeze / not in the mood).

## Using the Code

You can just fork the code and make your own adjustments. The solution works under the following assumptions:

- The trigger in Azure DevOps is a "build succeeded" trigger for a finished build job
- The referenced URL contains a `name` parameter yielding the package reference to update (currently only a single package can be updated per installed webhook)
- Only NuGet packages (and C# .NET SDK project files `.csproj`) are supported
- When the build succeeded the latest package is already available via the (Azure DevOps) NuGet feed

All adjustments can be done via the `Constants.cs` file. There are two environment variables:

| Variable      | Required?                       | Description                                                              |
|---------------|---------------------------------|--------------------------------------------------------------------------|
| `DEVOPS_ORGA` | No, has fallback                | The organization / name of the Azure DevOps account                      |
| `DEVOPS_PAT`  | Yes                             | The Personal Access Token with access to the NuGet feed and repositories |

Found a bug :eyes:? Report it in the issues or make a PR - any contribution appreciated :beers:!

## License

The project "DevOps Little Helper" is released using the MIT license. For more information see the [LICENSE file](LICENSE).

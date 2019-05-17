# DevOps Little Helper

This project is the companion for the [serverless challenge article on CodeProject](https://www.codeproject.com/Articles/3941198/Serverless-DevOps-Little-Helper#azure-devops-subscription-setup).

> This solution is designed for Azure Functions to give some value to Azure DevOps solutions.

If you find the solution useful you can deploy it via a click of the button below.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Motivation

I wanted to write a simple utility to automatically (propose an) update (of) references with respect to our internally developed / used libraries.

:zap: **Idea**

Whenever a new version of one of these libraries is published a webhook is trigged (Azure DevOps subscription). The triggered functionality inspects all available repositories and updates the references if found / outdated. The update is performed in form of a pull request.

:dollar: **Value Proposition**

We are notified in case of a new pull request and can accept the update or deny it due to whatever reasons (incompatibilities / feature freeze / not in the mood).

## Using the Code

### Deploy via Azure

The by far easiest way to set this is up is via the default deploy to Azure functionality.

The button at the top of the README will start the process. A deep link [is also available](https://azuredeploy.net/?repository=https://github.com/FlorianRappl/DevOpsLittleHelper).

### Direct Fork

You can just fork the code and make your own adjustments. The solution works under the following assumptions:

- The trigger in Azure DevOps is a "build succeeded" trigger for a finished build job
- The referenced URL contains a `name` parameter yielding the package reference to update (currently only a single package can be updated per installed webhook)
- An optional type query parameter determines the package type (by default dotnet is used)
- Currently supported package systems:
  - NuGet packages (and C# .NET SDK project files `.csproj`) are supported (type=dotnet)
  - NPM packages (using package.json) are supported (type=nodejs)
- When the build succeeded the latest package is already available via the (Azure DevOps) NuGet feed

All adjustments (for fallbacks) can be done via the `Constants.cs` file.

For most settings the provided environment variables should be sufficient:

| Variable            | Required?            | Description                                                                |
|---------------------|----------------------|----------------------------------------------------------------------------|
| `DEVOPS_PAT`        | Yes                  | The Personal Access Token with access to the NuGet feed and repositories   |
| `DEVOPS_ORGA`       | No (but recommended) | The organization / name of the Azure DevOps account                        |
| `DEVOPS_NEW_BRANCH` | No                   | The name of the new branch. See parameters below.                          |
| `DEVOPS_PR_TITLE`   | No                   | The title of the pull request to create (if any). See parameters below.    |
| `DEVOPS_PR_DESC`    | No                   | The description of the pull request to create (if any). Allows parameters. |
| `DEVOPS_COMMIT_MSG` | No                   | The commit message of the pull request to create (if any). No parameters.  |

The parameters to use in the provided strings are:

| Parameter       | Replacement        | Description                                         |
|-----------------|--------------------|-----------------------------------------------------|
| Package Name    | `{packageName}`    | The name of the package to update.                  |
| Package Version | `{packageVersion}` | The new version of the updated package.             |
| Package Suffix  | `{suffix}`         | A sanatized (dashed) package name - version string. |
| Dev Ops Version | `{appVersion}`     | The current version of Dev Ops Little Helper.       |

Found a bug :eyes:? Report it in the issues or make a PR - any contribution appreciated :beers:!

## License

The project "DevOps Little Helper" is released using the MIT license. For more information see the [LICENSE file](LICENSE).

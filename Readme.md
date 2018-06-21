# EFIngresProvider

An Entity Framework provider for Ingres.

## Notes

- This provider only works with Entity Framework 5
- Views are treated as tables in the DDEX provider
- The DDEX provider designates the first column as a primary key for tables and views that do not have a primary key.
- As of now, this provider has not been published to nuget.org.

## Build

Before building EFIngresProvider please:

- Make sure [Node.js](https://nodejs.org/) is installed.
- Make sure [nuget.exe](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) is in the path.
- Make sure Visual Studio 2017 is installed.
- Check the paths for `MSBuild.exe` and `devenv.com` in `Build.cmd`, and change them, if required.
- Set up tests as described below.

To build the EFIngresProvider run:

```
Build.cmd
```

This will produce

- `dist/EFIngresProvider.<version>.nupkg`   
  A NuGet package containing the provider

- `dist/EFIngresProviderVSIX.vsix`   
  An extension to enable the provider in Visual Studio - only works for Visual Studio 2017, as of now

- `dist/deploy/EFIngresProviderDeploy.exe`   
  Run this program as administrator to enable the provider in Visual Studio < 2017   
  _At some point this should be deprecated in favour of making the VSIX package work for Visual Studio 2015._

## Tests

To set up tests, add a file named `EFIngresProvider.Tests/TestConnection.json`, containing a connection string. For example:

```json
{
  "connectionString": "Server=mytestserver;Port=II7;Database=mytestdb;User ID=me;Password=my-password;VnodeUsage=connect"
}
```

This file is ignored by git, and should never be committed, as it will contain sensitive information.

After creating `EFIngresProvider.Tests/TestConnection.json`, and after any commit to git, please run:

```
setup.cmd
```

This script does two things:
- It changes the schema name for test tables in `EFIngresProvider.Tests/TestModel/TestModel.edmx` to the effective database user (`"Dbms_user"` or `"User ID"`) supplied in the connection string.
- It installs a pre-commit hook in the git repository, that resets the schema name to `"efingres"` for test tables in `EFIngresProvider.Tests/TestModel/TestModel.edmx`.

Be aware that the tests create and drop tables while running. So be sure to use an empty database, that is not used for anything else.

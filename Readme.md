﻿# EFIngresProvider

An Entity Framework provider for Ingres.

## Notes

- This provider works with Entity Framework 5
- Views are treated as tables in the DDEX provider
- The DDEX provider designates the first column as a primary key for tables and views that do not have a primary key.
- As of now, this provider has not been published to nuget.org.

## Build

Before building EFIngresProvider please:

- Make sure [nuget.exe](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) is in the path
- Make sure Visual Studio 2017 is installed
- Check the paths for `MSBuild.exe` and `devenv.com` in `Build.cmd`, and change them, if required.
- Set up tests as described below

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
  Run this program to enable the provider in Visual Studio < 2017

## Tests

To set up tests, add a file named `EFIngresProvider.Tests/TestConnection.json`, containing a connection string. F.ex.:

```json
{
  "connectionString": "Server=mytestserver;Port=II7;Database=mytestdb;User ID=me;Password=my-password;Timezone=EUROPE-CENTRAL;VnodeUsage=connect"
}
```

Be aware that the tests create and drop tables while running tests. So be sure to use an empty database, that is not used for anything else.

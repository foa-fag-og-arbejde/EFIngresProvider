# EFIngresProvider

An Entity Framework provider for Ingres.

## Notes

This provider only works with Entity Framework 5.

As of now, it has not been published to nuget.org.

## Build

To build the EFIngresProvider run:

```
Build.cmd
```

This will produce

* `dist/EFIngresProvider.<version>.nupkg`   
  A NuGet package containing the provider

* `dist/EFIngresProviderVSIX.vsix`   
  An extension to enable the provider in Visual Studio - only works for Visual Studio 2017, as of now

* `dist/deploy/EFIngresProviderDeploy.exe`   
  Run this program to enable the provider in Visual Studio < 2017

## Tests

To set up tests, add a file named `EFIngresProvider.Tests/TestConnection.json`, containing a connection string. F.ex.:

```json
{
  "connectionString": "Server=mytestserver;Port=II7;Database=mytestdb;User ID=me;Password=my-password;Timezone=EUROPE-CENTRAL;VnodeUsage=connect"
}
```

Be aware that the tests create and drop tables while running tests. So be sure to use an empty database, that is not used for anything else.
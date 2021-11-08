# Configuration Overview

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory. When developing Hive, the `appsettings.Development.json` file will be used instead.

This is a quick landing page for Hive's configuration. Further documentation can be found through their dedicated pages.

- [Serilog](https://github.com/serilog/serilog-settings-configuration)
- [Restricting Non-Authenticated Users](Configuration/GuestRestrictionMiddleware.md)
- [Auth0](Configuration/Auth0.md)
- [Uploads](Configuration/Uploads.md)

## Root Configuration Options

### `PathBase` - `string`

This represents the `PathBase` to set. See: [the microsoft documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.usepathbaseextensions.usepathbase?view=aspnetcore-5.0)

### `MaxFileSize` - `long`

The maximum file size in bytes. It must also be > 0. It will be set to: `32 * 1024 * 1024` on default.

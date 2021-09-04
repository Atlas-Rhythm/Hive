# Hive

A general backend project for modding communities.

## Building

### Dependencies

- Visual Studio >= 16.6
  - If not on Visual Studio Preview 2 or later, enable `.NET Core Preview` from `Tools/Options/Environment/Preview Features`

### PostgreSQL

- PostgreSQL >= 12.0
- Add a connection string with the name `Default` to your project secrets of Hive. This should be used for connecting to 
  Hive's specific database.
  - Example: `User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database=postgres;Pooling=true;`
- Add a connection string (without a database) to the name `Test` to your project secrets of Hive. This is used in Hive.Tests for creating test databases.
  - Example: `User ID=postgres;Password=postgres;Host=localhost;Port=5432;`

### Visual Studio Intellisense

Visual Studio may complain about certain things if you start it without first building the code generator.
To make sure intellisense is functional, run `dotnet build` before opening Visual Studio. It may also just
not work if you're not running the latest preview.

### Permission Rules

The permission system is a key component to Hive. Hive uses highly configurable rules to determine access for various components in Hive.
[Check out the dedicated documentation page for further information.](docs/Hive.Permissions/Usage.md).

### Plugins

See the documentation for [`Hive.Plugins`](docs/Hive.Plugins/) for information about writing and using plugins.

## Configuration

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory.

For configuration documentation, [see the dedicated documentation page.](docs/Hive/Configuration.md)
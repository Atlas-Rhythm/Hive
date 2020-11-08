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

- Rules are stored (persistently) in the filesystem. The location is configurable through `appsettings.json` (not yet supported).
- The file structure is:

```
| hive.rule
| hive/
|-> mod.rule
|-> mod/
|---> aditionalData.rule
```

### Plugins

- Plugins are loaded in a fashion TBD (most likely filesystem)

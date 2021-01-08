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
These rules are persistently stored in the file system. The permission system itself can be configured through `appsettings.json`, documented below.

[Check out the dedicated documentation page for further information.](https://github.com/Atlas-Rhythm/Hive/blob/master/Hive.Permissions/docs/Usage.md).

#### File Structure

Below is an example of the file structure for Hive's permission rules. Notice how subfolders are determined by the separator token.
For example, the file location for the rule `hive.mod.edit` will be `<Rules folder>/hive/mod/edit.rule`.
Higher level rules, such as `hive` and `hive.mod`, will also have their own definitions in the file system.

```
| hive.rule
| hive/
|-> mod.rule
|-> mod/
|---> edit.rule
|---> additionalData.rule
```

#### Disclaimer

Hive does *not* automatically generate rule files if they don't exist.
Rules with no defined definition in the file system are treated as if they do not exist, and are skipped.

### Plugins

- Plugins are loaded in a fashion TBD (most likely filesystem)

## Configuration

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory.

### Permissions Rules

A key component to Hive is its permission system, and the rules that govern it.

#### `RuleSubfolder`

This determines the subfolder in the Hive installation folder where permission rules will be read from. By default, this is set to a `Rules` subfolder.
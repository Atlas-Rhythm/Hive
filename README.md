# Hive

A general backend project for modding communities.

## Building

### Dependencies

- Visual Studio >= 16.6
  - Ensure .NET 5.0 is enabled
  - If on a version of Visual Studio earlier than Visual Studio Preview 1:
    - Ensure preview .NET is enabled, from `Tools/Options/Preview Features`)
  - If on a version of Visual Studio later than Visual Studio Preview 1:
    - .NET 5.0 is enabled by default

### Restoring Packages

- Go to `%appdata%\NuGet` and open the `NuGet.config`
- Open github, make a personal access token with at least `read:packages`
- Copy the following into the `configuration` node:

```xml
<packageSourceCredentials>
    <github>
    <add key="Username" value="your username here" />
    <add key="ClearTextPassword" value="your personal access token here" />
    </github>
</packageSourceCredentials>
```

- Change the `Username` to your own github username, and your `ClearTextPassword` to your newly made PAT.
- Reload Visual Studio.

#### FAQ

- When Visual Studio first opens the project, you may see a dialog box that asks for your username and password. **This is not useful!** Ignore this and instead add your configuration to your global configuration.
- If you have issues with restoring projects still, ensure you have placed the above snippet under the `configuration` node and that you have not mistyped your access token or username.

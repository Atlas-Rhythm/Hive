# Using Plugins

Plugins will be distributed as folders containing the main plugin assembly, its `.deps.json` file, and any dependencies and resources that it
requires. The name of the plugin assembly must match the name of the folder it is placed in. These folders are then placed in a location which is
configurable in the application configuration, which defaults to `plugins`.

### Configuring Plugins

In the app configuration, the configuration key `Plugins` contains the configuration for plugin loading, following this schema:
```js
{
    //   Whether or not to implicitly load all non-excluded plugins from the
    // configured plugin location. Defaults to true.
    "ImplicitlyLoadPlugins": bool,
    //   Whether or not to separate plugin configs from the main application
    // config. Defaults to false.
    "UsePluginSpecificConfig": bool,
    //   The path, relative to the working directory (or an absolute path),
    // of the directory containing all plugins that may be loaded. Defaults
    // to 'plugins'.
    "PluginPath": string,
    //   A list of plugins to load when ImplicitlyLoadPlugins is false.
    // Defaults to an empty array.
    "LoadPlugins": string[],
    //   A list of plugins to exclude from the loading process. Defaults to
    // an empty array. This applies whether or not ImplicitlyLoadPlugins is
    // true.
    "ExcludePlugins": string[],
    //   A map of plugin name to plugin configuration objects. Defaults
    // to an empty map. This is only used when UsePluginSpecificConfig is
    // false.
    "PluginConfigurations": Map<string, object>
}
```

When `UsePluginSpecificConfig` is true, each plugin's settings is loaded from `pluginsettings.json` in the plugin's directory, as well as loading
from enironment variables prefixed `PLUGIN_` followed by the name of the plugin with `.` replaced with `_`. When it is false, plugin settings will be
loaded from the values of the the `PluginConfiguration` object, where the key is the name of the plugin, and the value is the configuration object
to give the plugin.
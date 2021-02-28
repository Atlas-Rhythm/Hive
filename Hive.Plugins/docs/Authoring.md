# Authoring a Plugin

A Hive plugin, at its simplest, is a single assembly with a single empty type with the `[PluginStartup]` attribute. This type can be used in
much the same way as a 'startup' type in a typical ASP.NET Core application. 

#### The Startup Type

Each plugin, for it to actually do anything, must have at least one startup type, marked `[PluginStartup]`. There may be any number of these
types, and each will be instantiated.

The startup type may have a constructor that takes any of the following types:
- `Microsoft.Extensions.Configuration.IConfiguration` - A parameter of this type will receive an `IConfiguration` instance for the configuration
  for the plugin. Where this configuration ends up located is dependent on the configuration of the application, and should never be assumed by
  the plugin. See [Configuring Plugins](Using.md#Configuring_Plugins) for more information on the location of configuration.

  This is by far the most useful parameter type.
- `Microsoft.Extensions.Hosting.IHostEnvironment` - A parameter of this type will receive the `IHostEnvironment` instance provided by the host
  loading the plugin.
- `Hive.Plugins.Loading.PluginInstance` - A parameter of this type will receive the `PluginInstance` object for the plugin that this startup
  type is defined in. This provides easy access to the plugin assembly, plugin name, and plugin directory. (See [Using Plugins](Using.md) for
  details about the plugin name and directory.)
- `System.IO.DirectoryInfo` - A parameter of this type will recieve the `DirectoryInfo` object for the plugin's directory. This is equivalent
  to the `PluginDirectory` property on `PluginInstance`, and so is redundant with `PluginInstance` parameters.

The constructor may take no other parameter types.

The startup type may have exactly one method named `ConfigureServices`, which takes either no parameters, or a single `IServiceCollection`.
This method is called to allow the plugin to register services with dependency injection. This is the core method that Hive uses to allow
extensibility.

The startup type may have exactly one method named `Configure`, which may take any number of parameters, whose types are services available
to dependency injection, or additionally, `Microsoft.AspNetCore.Builder.IApplicationBuilder`. This method is used to configure the
application before it starts, so that it will behave correctly. One of the things that this method may do, for example, is register API route
handlers.

The plugin loader automatically injects each constructed startup instance into the service collection, along with their `PluginInstance`. This
means that your dependency injected types can request the instance of your startup class that was constructed, or you can request a list of
all loaded plugins.

#### Extensibility in Hive

Hive's extensibility is built primarily around dependency-injected plugin interfaces. You, as a plugin author, would implement a plugin interface
like `IUploadPlugin`, then register that type as an implementation of the interface during `ConfigureServices` in your plugin. Each plugin
interface provides a number of methods that act as hooks into their respective API surfaces. As an example, take the `IUploadPlugin` interface.
It has the following methods:
- `ValidateAndPopulateKnownMetadata` - Called for an uploaded mod with a `Stream` containing the uploaded file, to allow a plugin to 1) validate
  that the uploaded file is valid and 2) extract data about the mod out of that file to automatically populate fields.
- `LatePopulateKnownMetadata` - Called after the validation stage, to give a chance to populate metadata *after* validation, and after most
  plugins have done their data population. This is also only called if the file is valid according to all validations.
- `ValidateAndFixUploadedData` - Called during the final stage of the upload, after the user has corrected extracted data, allowing a plugin
  to both ensure that what the user entered is valid, and allowing it to perform some level of automatic correction.
- `UploadFinished` - Called after an upload is fully complete, to allow for webhooks and the like.

More often than not, these hooks are primarily for additional functionality rather than something akin to a permission check. There may be things
that current plugin interfaces do not currently provide. If possible, we ask that you make a feature request either that a hook that allows
that functionality be exposed, or that that functionality be directly added to Hive. 

#### Libraries

Plugins may ship any libraries they use, by placing them in their plugin directory alongside the main plugin assembly. Each plugin, if it uses
dependencies it ships with it, should have a `.deps.json` file along side the main assembly. Each plugin is loaded into its own
[AssemblyLoadContext](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext?view=net-5.0), and so libraries
should not cause problems between plugins. 

It is important to note that the plugin directory must have the same name as the plugin assembly (except for the extension), and the `.deps.json`
file must similarly have the same name.
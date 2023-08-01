# Hive Setup Guide

Follow this setup guide to create your own instance of the Hive back-end.

## Running Hive With Docker

If you are accustomed to using and operating Docker containers, running Hive through Docker may be appealing to you.

Included in the Hive repository is the [`docker-compose.yml`](https://github.com/Atlas-Rhythm/Hive/blob/master/docker-compose.yml) file. To run Hive in a Docker environment, please download this file.

**Do not use this file as-is!**

The Docker Compose file contains some configuration that should be changed before deployment. Please open `docker-compose.yml` in any text environment, and make any necessary modification and configuration to match your deployment environment.

When you are ready to deploy Hive, run `docker-compose up`.

## Downloading Hive

If you do not want to use Docker, you can download the latest release of Hive through [the Releases page](https://github.com/Atlas-Rhythm/Hive/releases).

Simply unzip the Hive release wherever you wish to host Hive.

**Be warned!** You may have to install the [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and [PostgreSQL 12](https://www.postgresql.org/) to run Hive outside of a Docker container.

## Downloading External Plugins

Plugins are an essential part of Hive. Hive comes with its [own set of core plugins](https://github.com/Atlas-Rhythm/HiveCorePlugins), which comes included with a Docker deployment of Hive.

If you wish to use third party plugins, or need to download Hive core plugins outside of a Docker environment, simply download the latest release from their respective repositories.

Hive expects the `Plugins` folder to be organized. Plugins *must* be included in a subdirectory of the same name, within the `Plugins` folder.

For example, using some of Hive's core plugins, your plugins folder should look like this:

```
Plugins/
|- Hive.FileSystemCdnProvider/
|  |- Hive.FileSystemCdnProvider.dll
|  |- ...
|
|- Hive.FileSystemRuleProvider/
|  |- Hive.FileSystemRuleProvider.dll
|  |- ...
|
|- Hive.Tags/
|  |- Hive.Tags.dll
|  |- ...
```

**Docker users be warned!** If you want to use third party plugins, you need to map a volume to `/app/plugins` in the Docker container.

## Setting up Auth0

An authentication service is required to use Hive. [Auth0](https://auth0.com/) is Hive's default authentication service. Unless you plan on using a separate authentication plugin, you need to setup Auth0.

[Follow our dedicated Auth0 guide](https://github.com/Atlas-Rhythm/Hive/tree/master/docs/Auth0) to set up Auth0 for your Hive installation. 

## Hive Configuration

Configuration for Hive's core is done through the `appsettings.json` file at the root of the Hive directory.

[We have dedicated documentation pages for configuring the various settings of Hive's core.](https://github.com/Atlas-Rhythm/Hive/blob/master/docs/Hive/Configuration.md)

By default, configuring Hive *plugins* is also done through `appsettings.json`, using a format that looks like this:

```json
{
	"Plugins": {
		"PluginConfigurations": {
			"Hive.FileSystemCdnProvider": {
				// Configuration for Hive.FileSystemCdnProvider
			},
			"Hive.FileSystemRuleProvider": {
				// Configuration for Hive.FileSystemRuleProvider
			},
			"Hive.Tags": {
				// Configuration for Hive.Tags
			}
			// ...and so on.
		}
	}
}```

Plugin configuration can also be done through environment variables, using the format `PLUGIN__<Plugin name, "." replaced with "_">__<Configuration key>`.

See the [Using Plugins](https://github.com/Atlas-Rhythm/Hive/blob/master/docs/Hive.Plugins/Using.md) page for more information on plugin configuration.
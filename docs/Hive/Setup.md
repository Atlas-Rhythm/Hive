# Hive Setup Guide

Follow this setup guide to create your own instance of the Hive back-end.

## Running Hive With Docker

If you are accustomed to using and operating Docker containers, running Hive through Docker may be appealing to you.

Included in the Hive repository is the [`docker-compose.yml`](https://github.com/Atlas-Rhythm/Hive/blob/master/docker-compose.yml) file. To run Hive in a Docker environment, please download this file.

**Do not use this file as-is!**

The Docker Compose file contains the password for the database. Change the password by opening `docker-compose.yml` in any text environment, and editing the `POSTGRES_PASSWORD` field wherever it comes up.

Furthermore, you will need to define a location on the host machine where Hive plugins will be stored and loaded from.

## Downloading Hive

If you do not want to use Docker, you can download the latest release of Hive through [the Releases page](https://github.com/Atlas-Rhythm/Hive/releases).

Simply unzip the Hive release wherever you wish to host Hive.

**Be warned!** You may have to install [PostgreSQL](https://www.postgresql.org/), the database used by Hive. Hive was developed with PostgreSQL 12. Later versions of PostgreSQL may work, but are untested.

## Downloading Plugins

Plugins are an essential part of Hive.

## Setting up Auth0

[Auth0](https://auth0.com/) is an authentication service that Hive supports by default.

An authentication service is required to use Hive. Unless you plan on using a separate authentication plugin, you need to setup Auth0.

[Follow our dedicated Auth0 guide](https://github.com/Atlas-Rhythm/Hive/tree/master/docs/Auth0) to set up Auth0 for your Hive installation. 
# syntax=docker/dockerfile:1.4
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /build

COPY ./Hive.sln .
COPY ./Directory.Build.props .
COPY ./Directory.Build.targets .
COPY ./NuGet.Config .
COPY ./NuGetInfo.props .

COPY ./src/Hive/Hive.csproj ./src/Hive/
COPY ./src/Hive.Plugins/Hive.Plugins.csproj ./src/Hive.Plugins/
COPY ./src/Hive.Analyzers/Hive.Analyzers.csproj ./src/Hive.Analyzers/
COPY ./src/CodeGen/Hive.CodeGen/Hive.CodeGen.csproj ./src/CodeGen/Hive.CodeGen/
COPY ./src/CodeGen/Hive.CodeGen.Attributes/Hive.CodeGen.Attributes.csproj ./src/CodeGen/Hive.CodeGen.Attributes/
COPY ./src/Hive.Utilities/Hive.Utilities.csproj ./src/Hive.Utilities/
COPY ./src/Hive.Versioning/Hive.Versioning.csproj ./src/Hive.Versioning/
COPY ./src/Hive.Permissions/Hive.Permissions.csproj ./src/Hive.Permissions/
COPY ./src/Hive.Dependencies/Hive.Dependencies.fsproj ./src/Hive.Dependencies/
RUN dotnet restore "src/Hive/Hive.csproj"

COPY ./.git ./.git/
COPY ./src ./src/
COPY ./tools ./tools/

WORKDIR /build/src/Hive
RUN dotnet publish "Hive.csproj" -c Release -o /app/publish --framework net6.0

COPY ./docker-entrypoint.sh /app/publish/docker-entrypoint.sh
RUN chmod +x /app/publish/docker-entrypoint.sh

# ---
FROM ghcr.io/atlas-rhythm/hivecoreplugins:master AS core-plugins

# ---
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

COPY --from=core-plugins /Plugins /app/core-plugins
COPY --from=build /app/publish .

EXPOSE 80
VOLUME ["/app/plugins"]

ARG GIT_REPO
LABEL org.opencontainers.image.source=${GIT_REPO}

ENTRYPOINT ["/app/docker-entrypoint.sh"]
CMD ["dotnet", "Hive.dll"]

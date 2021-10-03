#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["src/Hive/Hive.csproj", "src/Hive/"]
COPY ["src/Hive.Plugins/Hive.Plugins.csproj", "src/Hive.Plugins/"]
COPY ["src/Hive.Analyzers/Hive.Analyzers.csproj", "src/Hive.Analyzers/"]
COPY ["src/CodeGen/Hive.CodeGen/Hive.CodeGen.csproj", "src/CodeGen/Hive.CodeGen/"]
COPY ["src/CodeGen/Hive.CodeGen.Attributes/Hive.CodeGen.Attributes.csproj", "src/CodeGen/Hive.CodeGen.Attributes/"]
COPY ["src/Hive.Utilities/Hive.Utilities.csproj", "src/Hive.Utilities/"]
COPY ["src/Hive.Versioning/Hive.Versioning.csproj", "src/Hive.Versioning/"]
COPY ["src/Hive.Permissions/Hive.Permissions.csproj", "src/Hive.Permissions/"]
COPY ["src/Hive.Dependencies/Hive.Dependencies.fsproj", "src/Hive.Dependencies/"]
RUN dotnet restore "src/Hive/Hive.csproj"
COPY . .
WORKDIR "/src/src/Hive"
RUN dotnet build "Hive.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hive.csproj" -c Release -o /app/publish --framework net5.0

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hive.dll"]

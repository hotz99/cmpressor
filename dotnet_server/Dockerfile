# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./*.csproj ./dotnet_server/
RUN dotnet restore

# copy everything else and build app
COPY ./. ./dotnet_server/
WORKDIR /source/dotnet_server/
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "dotnet_server.dll"]

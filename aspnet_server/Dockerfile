# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./aspnet_server/*.csproj /source/aspnet_server/
COPY ../proto /source/proto
RUN dotnet restore /source/aspnet_server/aspnet_server.csproj

# copy everything else and build app
COPY ./aspnet_server/ /source/aspnet_server/
RUN dotnet publish /source/aspnet_server/aspnet_server.csproj -c release -o /app 

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "aspnet_server.dll"]

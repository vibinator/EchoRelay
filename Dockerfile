# Taken from https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore EchoRelay.Cli
# Build and publish a release
RUN dotnet publish EchoRelay.Cli -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "EchoRelay.Cli.dll"]
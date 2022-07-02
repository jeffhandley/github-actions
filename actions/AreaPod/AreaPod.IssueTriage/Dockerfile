# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-artifacts

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . /actions
WORKDIR "/actions"
RUN dotnet publish AreaPod.IssueTriage.csproj -c Release -o artifacts -r linux-x64 --sc

# Export an image based on the .NET SDK that has the build artifacts included
FROM ubuntu:latest
COPY --from=build-artifacts /actions/artifacts /actions

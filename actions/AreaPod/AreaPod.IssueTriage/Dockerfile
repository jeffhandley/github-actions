# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-artifacts

# Copy everything and publish the release (publish implicitly restores and builds)
COPY ./actions /actions
WORKDIR "/actions"
RUN dotnet publish /actions/AreaPod/AreaPod.IssueTriage/AreaAreaPod.IssueTriage.csproj -c Release -o /actions/artifacts

# Export an image based on the .NET SDK that has the build artifacts included
FROM mcr.microsoft.com/dotnet/sdk:7.0
COPY --from=build-artifacts /actions/artifacts /actions
ENTRYPOINT [ "dotnet", "/actions/AreaPod.IssueTriage.dll" ]

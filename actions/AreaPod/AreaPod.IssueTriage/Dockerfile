# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-artifacts

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . /actions
WORKDIR /actions
RUN dotnet publish AreaPod.IssueTriage.csproj -c Release -o artifacts -r ubuntu.20.04-x64 --sc

# Export an image based on the .NET SDK that has the build artifacts included
FROM mcr.microsoft.com/dotnet/runtime-deps:7.0
COPY --from=build-artifacts /actions/artifacts /actions
ENTRYPOINT [ "/actions/AreaPod.IssueTriage" ]

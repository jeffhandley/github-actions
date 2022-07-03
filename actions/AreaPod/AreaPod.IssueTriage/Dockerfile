# Create a build-artifacts image that has the .NET SDK installed
FROM mcr.microsoft.com/dotnet/sdk:7.0 as actions-build

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

# Copy the contents of the local folder into the /actions folder in the image
COPY . /actions-build
WORKDIR /actions-build

# Build/Publish the self-contained NativeAOT image for ubuntu:20.04
RUN dotnet publish -c Release -o artifacts -r ubuntu.20.04-x64 --sc -p:PublishAOT=true -p:PublishTrimmed=true -p:AssemblyName=areapod-issuetriage

# Export an image based on ubuntu:20.04 that has the /actions/areapod-issuetriage executable added
FROM ubuntu:20.04
COPY --from=actions-build /actions-build/artifacts/areapod-issuetriage /actions/areapod-issuetriage
ENV PATH=$PATH:/actions

ENTRYPOINT [ "/actions/areapod-issuetriage" ]

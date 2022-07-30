ARG ACTION_NAME
ARG ACTION_FOLDER

# Create an actions-build image that has the .NET SDK installed
FROM mcr.microsoft.com/dotnet/sdk:7.0 as actions-build

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

# Copy the contents of the local folder into the /actions folder in the image
COPY . /actions-build
WORKDIR /actions-build

# Build/Publish the self-contained NativeAOT image for ubuntu:20.04
RUN dotnet publish -c Release -o artifacts -r ubuntu.20.04-x64 --sc ${ACTION_FOLDER}

# Export an image that includes the .NET native dependencies as well as the build artifacts
FROM mcr.microsoft.com/dotnet/runtime-deps:7.0
COPY --from=actions-build /actions-build/artifacts /actions

ENV PATH=$PATH:/actions
ENTRYPOINT [ "/actions/${ACTION_NAME}" ]

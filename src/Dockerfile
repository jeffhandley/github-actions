# Create an actions-build image that has the .NET SDK installed
FROM mcr.microsoft.com/dotnet/sdk:7.0 as actions-build
ARG ACTION_FOLDER
ARG ACTION_NAME

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

# Copy the contents of the local folder into the /actions folder in the image
COPY . /actions-build
WORKDIR /actions-build

# Build/Publish the self-contained NativeAOT image for ubuntu:20.04
RUN dotnet publish -c Release -o artifacts -r ubuntu.20.04-x64 --sc $ACTION_FOLDER

# Export an image that includes the .NET native dependencies as well as the build artifacts
FROM mcr.microsoft.com/dotnet/runtime-deps:7.0
ARG ACTION_NAME

COPY --from=actions-build /actions-build/artifacts /actions

# Create an run-action.sh that will invoke the action (since entrypoints cannot expand arguments)
RUN echo "#!/bin/bash\n/actions/$ACTION_NAME \$@\n" > /actions/run-action.sh
RUN chmod +x /actions/run-action.sh

ENTRYPOINT [ "/actions/run-action.sh" ]


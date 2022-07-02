# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . /actions
WORKDIR "/actions"
RUN dotnet publish AreaPod.IssueTriage.csproj -c Release -o artifacts

# Label the container
LABEL maintainer="Jeff Handley <jeffhandley@users.noreply.github.com>"
LABEL repository="https://github.com/jeffhandley/action-playground"
LABEL homepage="https://github.com/jeffhandley/action-playground"

# Label as GitHub action
LABEL com.github.actions.name=".NET Org Area Pod Issue Triage"
LABEL com.github.actions.description="A GitHub action that drives the .NET org's area pod issue triage process"
LABEL com.github.actions.color="purple"

# Export an image based on the .NET Runtime that has the build-env artifacts included
FROM mcr.microsoft.com/dotnet/runtime:7.0
COPY --from=build-env /actions/artifacts /actions
ENTRYPOINT [ "/actions/AreaPod.IssueTriage.exe" ]

# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as dotnet-sdk

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . /action
WORKDIR "/action"
RUN dotnet publish AreaPod.IssueTriage.csproj -c Release -o artifacts --no-self-contained

# Label the container
LABEL maintainer="Jeff Handley <jeffhandley@users.noreply.github.com>"
LABEL repository="https://github.com/jeffhandley/action-playground"
LABEL homepage="https://github.com/jeffhandley/action-playground"

# Label as GitHub action
LABEL com.github.actions.name=".NET Org Area Pod Issue Triage"
LABEL com.github.actions.description="A GitHub action that drives the .NET org's area pod issue triage process"
LABEL com.github.actions.color="purple"

# Relayer the .NET SDK with the build output
FROM mcr.microsoft.com/dotnet/sdk:7.0
COPY --from=dotnet-sdk /action/artifacts /action
ENTRYPOINT [ "dotnet" "/AreaPod.IssueTriage.dll" ]

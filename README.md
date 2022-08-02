This commit of the repo includes a `backlog` action that was a stub for an action that would query a repo's issues and look for reactions on issues to label them as "trending".

Here are the components involved:

1. A .NET console application that will be the implementation of the action logic
    * [`src/Backlog/Backlog.csproj`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/src/Backlog/Backlog.csproj)
        * Console application project file
        * Includes package references to System.CommandLine and Octokit.GraphQL
        * Defines AOT and Trimming optimization configurations that light up when published for a specific runtime identifier
    * [`src/Backlog/Program.cs`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/src/Backlog/Program.cs)
        * Console application program file
        * Consumes `GITHUB_TOKEN` and `GITHUB_REPOSITORY` environment variables
        * Defines a `trending-issues` sub-command for the console application, accepting option arguments for `--reactions`, `--days`, and `--label`
        * The `HandleTrendingIssues` sub-command handler echos the arguments to the console for demo purposes
        * That's where the logic would go for using Octokit.GraphQL to query issues and their reactions to then use business logic to define which issues need to get labeled as trending (with a mutation)
2. Build infrastructure to produce a Docker image that runs the console application
    * [`src\Dockerfile`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/src/Dockerfile)
        * A parameterized Dockerfile that produces a Docker image for the specified action console application
        * Expects `ACTION_FOLDER` and `ACTION_NAME` arguments to be supplied to `docker build` using `--build-args`
        * `ACTION_FOLDER` is the case-sensitive subfolder name of the action console application under the `src` folder; e.g. `"Backlog"`
        * `ACTION_NAME` is the lower-kebab-case name of the action to be produced; e.g. `"backlog"`
        * The image will be produced using `mcr.microsoft.com/dotnet/runtime-deps:7.0` as the base image, containing an `/actions/backlog` executable, with an `/actions/run-action.sh` entrypoint script that invokes the console application
    * [`.github/workflows/publish-action.backlog.yml`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/.github/workflows/publish-action.backlog.yml)
        * A GitHub workflow that can be manually triggered to build and publish the `backlog` action's Docker image to the ghcr.io registry
        * Uses the parameterized [`.github/workflows/publish-action.yml`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/.github/workflows/publish-action.yml) reusable workflow
3. A GitHub action that creates a Docker container to execute the `backlog` action console application for the `trending-issues` sub-command
    * [`actions/backlog/trending-issues/action.yml`](https://github.com/jeffhandley/github-actions/tree/with-backlog-action/actions/backlog/trending-issues/action.yml)
    * Defines the inputs needed to execute the console application
    * Runs the action using `docker`, referencing the published `"docker://ghcr.io/jeffhandley/github-actions/backlog:main"` Docker image
    * Passes the input argments into the Docker command using the command-line arguments as defined in the console application
    * Adds the `GITHUB_TOKEN` value into the Docker container's environment variables, sourced by the `github.token` context value from the executing workflow
4. A GitHub workflow that executes the GitHub action for `backlog/trending-issues`
    * [`.github/workflows/backlog.trending-issues.yml`](https://github.com/jeffhandley/github-actions/blob/with-backlog-action/.github/workflows/backlog.trending-issues.yml)
    * Defines a `schedule` trigger using a crontab of `'* * * * *'`, which results in GitHub executing this workflow automatically as often as GitHub will do so (documented as "no more than every 5 minutes")
    * Defines a `workflow_call` trigger that accepts inputs from a calling workflow for the `reaction`, `days`, and `label` values; _**this is what makes the workflow reusable from other workflows/repositories**_
    * Defines a `workflow_dispatch` trigger that accepts inputs; _**this is what makes the workflow available for manual trigger from GitHub's UI**_
    * Defines a job that runs on `ubuntu-latest`, with a single step that invokes the `jeffhandley/github-actions/actions/backlog/trending-issues@main` action, passing the `reactions`, `days`, and `label` arguments
    * This results in execution of the action as defined in [`https://github.com/jeffhandley/github-actions/tree/main/actions/backlog/trending-issues/action.yml`](https://github.com/jeffhandley/github-actions/tree/main/actions/backlog/trending-issues/action.yml)
5. GitHub repository configuration of the "Actions permissions"
    * "Allow jeffhandley, and select non-jeffhandley, actions and reusable workflows"
    * Uncheck "Allow actions created by GitHub"
    * Uncheck "Allow actions by Marketplace"
    * Specify the following "Patterns": (the trailing commas are required)
        * actions/checkout@v3,
        * docker/build-push-action@v3,
        * docker/login-action@v2,
        * docker/metadata-action@v4,
        * docker://ghcr.io/jeffhandley/github-actions/*,
        * jeffhandley/github-actions/*,

With all of this in place within this repository, other repositories can now invoke the `backlog` commands and the `trending-issues` action specifically. The following is needed within the "subscribed" repository to achieve that:

### GitHub workflow definition
**`.github/workflows/backlog.trending-issues.yml`**

1. Define a `schedule` (where the configured parameters will be used)
2. Define a `workflow_dispatch` for a manual trigger with GitHub's UI
3. Define a `backlog` job that invokes the reusable workflow defined at `jeffhandley/github-actions/.github/workflows/backlog.trending-issues.yml@with-backlog-action`
4. Specify the `reactions`, `days`, and `label` arguments, using the `workflow_dispatch` inputs if specified, or the configured defaults to use when triggered on schedule
    * This works using `inputs.<arg> || <default>` expressions
    * Note the use of `fromJson(inputs.<arg>)` to force the UI-entered values to numbers

```yml
# This workflow looks for trending issues and applies a label to them
# Issues are considered trending if they receive a significant number
# of positive reactions within a period of time

name: "Backlog: Trending Issues"

on:
  schedule:
  - cron: '* * * * *'

  workflow_dispatch:
    inputs:
      reactions:
        description: The minimum number of positive reactions (from unique users)
        required: true
        type: number
        default: 5
      days:
        description: The maximum number of days within which the reactions need to occur
        required: true
        type: number
        default: 7
      label:
        description: The label to apply for trending issues
        required: true
        type: string
        default: 'trending'

jobs:
  backlog:
    uses: jeffhandley/github-actions/.github/workflows/backlog.trending-issues.yml@with-backlog-action
    with:
      reactions: ${{ fromJson(inputs.reactions) || 5 }}
      days:      ${{ fromJson(inputs.days)      || 7 }}
      label:     ${{ inputs.label               || 'trending' }}
```

With this in place, the subscribed repository will invoke the trending-issues backlog action on a schedule and also allow manual triggering of that action through the GitHub UI.

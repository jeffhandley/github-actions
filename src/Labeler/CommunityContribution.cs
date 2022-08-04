using Labeler.Queries;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using static Octokit.GraphQL.Variable;

namespace Labeler;

internal class CommunityContribution
{
    public static Command GetActionCommand(string actionName)
    {
        var prArg = new Option<uint>("--pr", "The pull request number to process") { IsRequired = true };
        var labelArg = new Option<string>("--label", "The label to apply for community-authored pull requests") { IsRequired = true };

        var ignoreBotsArg = new Option<bool>("--ignore-apps", () => true, "Ignore pull requests authored by apps");
        var ignoreTeamArg = new Option<string?>("--ignore-team", "A GitHub team containing authors to ignore");
        var ignoreAuthorsArg = new Option<List<string>?>("--ignore-authors", "A list of authors to ignore")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var testArg = new Option<bool>("--test", "Run in test mode without performing any updates") { IsHidden = true };
        var testAuthorArg = new Option<string?>("--test-author", "The author to simulate for the pull request for testing") { IsHidden = true };

        var communityPrsCommand = new Command(actionName, "Label issues authored by specified users") {
            prArg, labelArg, ignoreBotsArg, ignoreAuthorsArg, ignoreTeamArg, testArg, testAuthorArg
        };

        communityPrsCommand.SetHandler(HandlePullRequest,
            prArg, labelArg, ignoreBotsArg, ignoreTeamArg, ignoreAuthorsArg, testArg, testAuthorArg
        );

        return communityPrsCommand;
    }

    static async Task HandlePullRequest(uint prNumber, string label, bool ignoreBots, string? ignoreTeam, List<string>? ignoreAuthors, bool isTestMode, string? testAuthor)
    {
        string[] ownerRepo = Program.GITHUB_REPOSITORY.Split('/');
        string owner = ownerRepo[0];
        string repo = ownerRepo[1];

        Console.WriteLine($"Handling Pull Request Event");
        Console.WriteLine($"  PR:             {owner}/{repo}#{prNumber}");
        Console.WriteLine($"  Label:          {label}");
        Console.WriteLine($"  Ignore Bots:    {ignoreBots}");
        Console.WriteLine($"  Ignore Team:    {ignoreTeam}");
        Console.WriteLine($"  Ignore Authors: {(ignoreAuthors?.Any() == true ? string.Join(", ", ignoreAuthors) : "")}");

        var appInfo = new ProductHeaderValue("Labeler.CommunityPullRequests");
        var connection = new Connection(appInfo, Program.GITHUB_TOKEN);

        var dataQuery = new Query()
            .Repository(Var("repo"), Var("owner"))
            .Select(repository => new
            {
                LabelId = repository.Label(Var("label")).Select(l => l.Id).SingleOrDefault(),
                PullRequest = repository.PullRequest(Var("pr_number"))
                    .Select(pr => new
                    {
                        pr.Id,
                        pr.Number,
                        pr.Closed,
                        Labels = pr.Labels(null, null, null, null, new LabelOrder { Field = LabelOrderField.Name, Direction = OrderDirection.Asc })
                            .AllPages()
                            .Select(label => label.Name)
                            .ToList(),
                        Author = pr.Author.Select(author => new
                        {
                            author.Login,
                            author.ResourcePath
                        }).Single()!
                    }).SingleOrDefault()
            }).Compile();

        var data = await connection.Run(dataQuery, new Dictionary<string, object>
        {
            { "owner", owner },
            { "repo", repo },
            { "pr_number", prNumber },
            { "label", label },
        });

        if (string.IsNullOrEmpty(data.LabelId.Value))
        {
            throw new ApplicationException($"Label '{label}' could not be found. Aborting.");
        }

        // Allow a test author to be specified, overriding the PR's author
        var author = (isTestMode ? testAuthor : null) ?? data.PullRequest.Author.Login;
        var authorIsApp = data.PullRequest.Author.ResourcePath.StartsWith("/apps/", StringComparison.InvariantCultureIgnoreCase);

        Console.WriteLine($"PR {owner}/{repo}#{data.PullRequest.Number}:");
        Console.WriteLine($"  State:  {(data.PullRequest.Closed ? "Closed" : "Open")}");
        Console.WriteLine($"  Author: {author}{(authorIsApp ? "[bot]" : "")}");
        Console.WriteLine($"  Labels: {string.Join(", ", data.PullRequest.Labels)}");

        if (ignoreAuthors?.Contains(author, StringComparer.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine($"Author '{author}' is ignored.");
            return;
        }

        if (ignoreBots && authorIsApp)
        {
            Console.WriteLine($"Author '{author}' is ignored as a bot.");
            return;
        }

        if (ignoreTeam is not null)
        {
            var isTeamMember = await TeamQueries.QueryIsTeamMember(connection, owner, ignoreTeam, author);

            if (isTeamMember)
            {
                Console.WriteLine($"Author '{author}' is ignored as a member of team '{ignoreTeam}'.");
                return;
            }
        }

        var permission = await UserQueries.QueryUserRepositoryPermission(connection, owner, repo, author);

        var isCommunityContributor = (
            permission is null ||
            permission == RepositoryPermission.Read ||
            permission == RepositoryPermission.Triage
        );

        Console.WriteLine($"Collaborator:");
        Console.WriteLine($"  Permission: {permission?.ToString() ?? "<none>"}");
        Console.WriteLine($"  Community:  {isCommunityContributor}");

        if (!data.PullRequest.Labels.Contains(label, StringComparer.OrdinalIgnoreCase))
        {
            if (isCommunityContributor)
            {
                if (!isTestMode)
                {
                    var addLabel = new Mutation()
                        .AddLabelsToLabelable(new AddLabelsToLabelableInput { LabelableId = data.PullRequest.Id, LabelIds = new[] { data.LabelId } })
                        .Select(result => result.ClientMutationId)
                        .Compile();

                    await connection.Run(addLabel);
                }

                Console.WriteLine($"Author '{author}' is a community contributor. Label '{label}' added.");
            }
            else
            {
                Console.WriteLine($"Author '{author}' is not a community contributor.");
            }
        }
        else
        {
            if (isCommunityContributor)
            {
                Console.WriteLine($"Author '{author}' is a community contributor, but '{label}' is already applied.");
            }
            else
            {
                Console.WriteLine($"Author '{author}' is not a community contributor, but '{label}' is already applied.");
            }
        }
    }
}

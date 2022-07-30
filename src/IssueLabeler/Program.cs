using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using static Octokit.GraphQL.Variable;
using static System.Environment;
using static System.Collections.Specialized.BitVector32;

namespace IssueLabeler;

internal class Program
{
    static string GITHUB_TOKEN => GetEnvironmentVariable("GITHUB_TOKEN")!;
    static string GITHUB_REPOSITORY => GetEnvironmentVariable("GITHUB_REPOSITORY")!;

    static async Task<int> Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(GITHUB_TOKEN) || string.IsNullOrWhiteSpace(GITHUB_REPOSITORY))
        {
            throw new ArgumentException("Missing environment variable. GITHUB_ACTOR, GITHUB_TOKEN, and GITHUB_REPOSITORY are required.");
        }

        var issueArg = new Option<uint>("--issue", "The issue number to process")
        {
            IsRequired = true
        };

        var authorsArg = new Option<List<string>>("--authors", "The authors whose issues get labeled")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        var labelArg = new Option<string>("--label", "The label to apply if the issue was authored by one of the specified users")
        {
            IsRequired = true
        };

        var authorIssuesCommand = new Command("author-issues", "Label issues authored by specified users") { issueArg, authorsArg, labelArg };
        authorIssuesCommand.SetHandler(HandleAuthorIssues, issueArg, authorsArg, labelArg);

        var rootCommand = new RootCommand("Issue Labeler commands") { authorIssuesCommand };

        return await rootCommand.InvokeAsync(args);
    }

    static async Task HandleAuthorIssues(uint issueNumber, List<string> authors, string label)
    {
        string[] ownerRepo = GITHUB_REPOSITORY.Split('/');
        string owner = ownerRepo[0];
        string repo = ownerRepo[1];

        Console.WriteLine($"Handling Issue Event");
        Console.WriteLine($"  Issue:    {owner}/{repo}#{issueNumber}");
        Console.WriteLine($"  Authors:  {string.Join(", ", authors)}");
        Console.WriteLine($"  Label:    {label}");

        var appInfo = new ProductHeaderValue("AreaPods");
        var connection = new Connection(appInfo, GITHUB_TOKEN);

        var issueQuery = new Query()
            .Repository(Var("repo"), Var("owner"))
            .Issue(Var("issue_number"))
            .Select(issue => new
            {
                issue.Id,
                issue.Number,
                issue.Closed,
                Labels = issue.Labels(null, null, null, null, new LabelOrder { Field = LabelOrderField.Name, Direction = OrderDirection.Asc })
                    .AllPages()
                    .Select(label => label.Name)
                    .ToList(),
                Author = issue.Author.Select(author => author.Login).Single()!,
            }).Compile();

        var issue = await connection.Run(issueQuery, new Dictionary<string, object>
        {
            { "owner", owner },
            { "repo", repo },
            { "issue_number", issueNumber }
        });

        Console.WriteLine($"Issue {owner}/{repo}#{issue.Number} was processed");
        Console.WriteLine($"  State: {(issue.Closed ? "Closed" : "Open")}");
        Console.WriteLine($"  Labels: {string.Join(", ", issue.Labels)}");
        Console.WriteLine($"  Author: {issue.Author}");

        var needsLabel = authors.Contains(issue.Author, StringComparer.OrdinalIgnoreCase) && !issue.Labels.Contains(label, StringComparer.OrdinalIgnoreCase);

        if (needsLabel)
        {
            var labelQuery = new Query()
                .Repository(Var("repo"), Var("owner"))
                .Label(label)
                .Select(l => l.Id)
                .Compile();

            ID labelId = await connection.Run(labelQuery, new Dictionary<string, object>
            {
                { "owner", owner },
                { "repo", repo },
                { "label", label }
            });

            if (string.IsNullOrEmpty(labelId.Value))
            {
                throw new ApplicationException($"Label '{label}' could not be found. Aborting.");
            }

            var addLabel = new Mutation()
                .AddLabelsToLabelable(new AddLabelsToLabelableInput { LabelableId = issue.Id, LabelIds = new[] { labelId } })
                .Select(result => result.ClientMutationId)
                .Compile();

            await connection.Run(addLabel);

            Console.WriteLine($"  Issue author matched and the issue needed the label. '{label}' was added.");
        }
    }
}
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using static Octokit.GraphQL.Variable;
using Env = System.Environment;

namespace AreaPod.IssueTriage;

internal class Program
{
    static string GITHUB_TOKEN => Env.GetEnvironmentVariable("GITHUB_TOKEN")!;

    static async Task Main(string[] args)
    {
        string? GITHUB_TOKEN = Env.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrWhiteSpace(GITHUB_TOKEN))
        {
            throw new ArgumentException("Missing environment variable: GITHUB_TOKEN");
        }
        
        var issueArg = new Option<uint>(new[] { "--issue", "-i" }, "The issue number to process") { IsRequired = true };
        var issueCommand = new RootCommand("Area Pod issue triage");
        issueCommand.Add(issueArg);
        issueCommand.SetHandler(GetIssue, issueArg);

        await issueCommand.InvokeAsync(args);
    }

    static async Task GetIssue(uint issueNumber)
    {
        Console.WriteLine($"Processing issue number: {issueNumber}");

        try
        {
            var appInfo = new ProductHeaderValue("AreaPod.IssueTriage");
            var connection = new Connection(appInfo, GITHUB_TOKEN);

            var query = new Query()
                .RepositoryOwner(Var("owner"))
                .Repository(Var("repo"))
                .Issue(Var("issue_number"))
                .Select(issue => new
                {
                    issue.Number,
                    issue.Title,
                    issue.Closed,
                    issue.State,
                    issue.AuthorAssociation,
                    Milestone = issue.Milestone.Select(milestone => milestone.Title).SingleOrDefault(),
                    Author = issue.Author.Select(author => author.Login).Single(),
                    Labels = issue
                        .Labels(null, null, null, null, new LabelOrder { Field = LabelOrderField.Name, Direction = OrderDirection.Asc })
                        .AllPages()
                        .Select(label => label.Name).ToList()
                }).Compile();

            var values = new Dictionary<string, object>
            {
                { "owner", "jeffhandley" },
                { "repo", "action-playground" },
                { "issue_number", issueNumber }
            };

            var issue = await connection.Run(query, values);
            Console.WriteLine($"Fetched issue {issue.Number}: {issue.Title}. Labels: {string.Join(", ", issue.Labels)}");
        }
        catch
        {
            throw;
        }
    }
}

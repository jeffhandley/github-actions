using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using static Octokit.GraphQL.Variable;
using Env = System.Environment;
using AreaPod.IssueTriage.Models;
using System.Linq;

namespace AreaPod.IssueTriage;

internal class Program
{
    static string GITHUB_TOKEN => Env.GetEnvironmentVariable("GITHUB_TOKEN")!;

    static async Task<int> Main(string[] args)
    {
        string? GITHUB_TOKEN = Env.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrWhiteSpace(GITHUB_TOKEN))
        {
            throw new ArgumentException("Missing environment variable: GITHUB_TOKEN");
        }
        
        var issueArg = new Option<uint>(new[] { "--issue", "-i" }, "The issue number to process") { IsRequired = true };
        var actionArg = new Option<IssueAction?>(new[] { "--action", "-a" }, "The issue action");

        var triageCommand = new RootCommand("Area Pod issue triage");
        triageCommand.Add(issueArg);
        triageCommand.Add(actionArg);
        triageCommand.SetHandler(HandleIssueTriage, issueArg, actionArg);

        return await triageCommand.InvokeAsync(args);
    }

    static async Task HandleIssueTriage(uint issueNumber, IssueAction? action)
    {
        Console.WriteLine($"Processing issue number: {issueNumber}");

        var appInfo = new ProductHeaderValue("AreaPod.IssueTriage");
        var connection = new Connection(appInfo, GITHUB_TOKEN);

        var query = new Query()
            .RepositoryOwner(Var("owner"))
            .Repository(Var("repo"))
            .Issue(Var("issue_number"))
            .Select(issue => new IssueForTriage
            {
                Id = issue.Id,
                Number = issue.Number,
                Closed = issue.Closed,
                Milestone = issue.Milestone.Select(milestone => milestone.Title).SingleOrDefault(),
                Labels = issue
                    .Labels(null, null, null, null, new LabelOrder { Field = LabelOrderField.Name, Direction = OrderDirection.Asc })
                    .AllPages()
                    .Select(label => label.Name)
                    .ToList(),
                Author = issue.Author.Select(author => author.Login).Single()!,
                AuthorAssociation = issue.AuthorAssociation,
                ProjectColumns = issue
                    .ProjectCards(null, null, null, null, null)
                    .AllPages()
                    .Select(card => new ProjectCardClassic
                    {
                        Id = card.Id,
                        ProjectName = card.Project.Name,
                        ProjectNumber = card.Project.Number,
                        ColumnId = card.Column.Select(column => column.Id).SingleOrDefault(),
                        ColumnName = card.Column.Select(column => column.Name).SingleOrDefault(),
                        IsArchived = card.IsArchived,
                    })
                    .ToList(),
            }).Compile();

        var values = new Dictionary<string, object>
        {
            { "owner", "jeffhandley" },
            { "repo", "action-playground" },
            { "issue_number", issueNumber }
        };

        var issue = await connection.Run(query, values);

        Console.WriteLine($"Issue {issue.Number} was {action?.ToString() ?? "processed"}");
        Console.WriteLine($"  State: {(issue.Closed ? "Closed" : "Open")}");
        Console.WriteLine($"  Milestone: {issue.Milestone ?? "<none>"}");
        Console.WriteLine($"  Labels: {string.Join(", ", issue.Labels)}");
        Console.WriteLine($"  Author: {issue.Author}");
        Console.WriteLine($"  Author Association: {issue.AuthorAssociation.ToString()}");
        Console.WriteLine($"  Needs Triage: {(issue.NeedsTriage ? "Yes" : "No")}");
        
        foreach (var pc in issue.ProjectColumns)
        {
            Console.WriteLine($"  On Project '{pc.ProjectName}' ({pc.ProjectNumber}) in Column '{pc.ColumnName}'{(pc.IsArchived ? " [Archive]" : "")}");
        }

        if (issue.ProjectColumns.Any())
        {
            var card = issue.ProjectColumns.First();
            var archive = new UpdateProjectCardInput { ProjectCardId = card.Id, IsArchived = !card.IsArchived };
            var mutation = new Mutation()
                .UpdateProjectCard(Var("card"))
                .Select(result => new
                {
                    result.ProjectCard.IsArchived
                })
                .Compile();

            var mutationValues = new Dictionary<string, object>
            {
                { "card", archive }
            };

            var result = await connection.Run(mutation, mutationValues);
            Console.WriteLine($"    Card State Updated: {(result.IsArchived ? "Archived" : "Unarchived")}");
        }
    }
}

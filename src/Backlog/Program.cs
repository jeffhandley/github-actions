using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using static Octokit.GraphQL.Variable;
using static System.Environment;

namespace Backlog;

internal class Program
{
    static string GITHUB_TOKEN => GetEnvironmentVariable("GITHUB_TOKEN")!;
    static string GITHUB_REPOSITORY => GetEnvironmentVariable("GITHUB_REPOSITORY")!;

    static async Task<int> Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(GITHUB_TOKEN) || string.IsNullOrWhiteSpace(GITHUB_REPOSITORY))
        {
            throw new ArgumentException("Missing environment variable. GITHUB_TOKEN and GITHUB_REPOSITORY are required.");
        }

        var reactionsArg = new Option<uint>("--reactions", "The minimum number of reactions for trending consideration") { IsRequired = true };
        var daysArg = new Option<uint>("--days", "The maximum number of days for the reactions to occur within") { IsRequired = true };
        var labelArg = new Option<string>("--label", "The label to add to trending issues") { IsRequired = true };

        var trendingIssuesCommand = new Command("trending-issues", "Trending Issues") { reactionsArg, daysArg, labelArg };
        trendingIssuesCommand.SetHandler(HandleTrendingIssues, reactionsArg, daysArg, labelArg);

        var rootCommand = new RootCommand("Backlog commands") { trendingIssuesCommand };

        return await rootCommand.InvokeAsync(args);
    }

    static async Task HandleTrendingIssues(uint reactions, uint days, string label)
    {
        string[] ownerRepo = GITHUB_REPOSITORY.Split('/');
        string owner = ownerRepo[0];
        string repo = ownerRepo[1];

        Console.WriteLine($"Handling Trending Issues");
        Console.WriteLine($"  Repo:      {owner}/{repo}");
        Console.WriteLine($"  Reactions: {reactions}");
        Console.WriteLine($"  Days:      {days}");
        Console.WriteLine($"  Label:     {label}");
    }
}

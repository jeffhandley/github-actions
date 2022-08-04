using System;
using System.CommandLine;
using System.Threading.Tasks;
using static System.Environment;

namespace IssueLabeler;

internal class Program
{
    public static string GITHUB_TOKEN => GetEnvironmentVariable("GITHUB_TOKEN")!;
    public static string GITHUB_REPOSITORY => GetEnvironmentVariable("GITHUB_REPOSITORY")!;

    static async Task<int> Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(GITHUB_TOKEN) || string.IsNullOrWhiteSpace(GITHUB_REPOSITORY))
        {
            throw new ArgumentException("Missing environment variable. GITHUB_ACTOR, GITHUB_TOKEN, and GITHUB_REPOSITORY are required.");
        }

        var rootCommand = new RootCommand("Issue Labeler actions") {
            Authors.GetActionCommand("authors"),
            CommunityPullRequests.GetActionCommand("community-contribution")
        };

        return await rootCommand.InvokeAsync(args);
    }
}

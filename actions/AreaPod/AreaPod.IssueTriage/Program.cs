using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace AreaPod.IssueTriage;

internal class Program
{
    static async Task Main(string[] args)
    {
        string? GITHUB_TOKEN = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        
        var issueArg = new Option<int?>(new[] { "--issue", "-i" }, "The issue number to process") { IsRequired = true };
        var issueCommand = new RootCommand("Area Pod issue triage");
        issueCommand.Add(issueArg);
        issueCommand.SetHandler(issue => Console.WriteLine($"Processing issue number: {issue}"), issueArg);

        await issueCommand.InvokeAsync(args);
    }
}

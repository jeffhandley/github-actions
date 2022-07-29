#nullable disable warnings

using System.Collections.Generic;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

namespace AreaPods.Models;

internal class IssueForTriage
{
    public ID Id { get; set; }
    public int Number { get; init; }
    public string Title { get; init; }
    public bool Closed { get; init; }
    public string? Milestone { get; init; }

    public List<string> Labels { get; init; }
    public string Author { get; init; }
    public List<ProjectCard_v1> ProjectCards_v1 { get; init; }

    public CommentAuthorAssociation? AuthorAssociation { get; init; }
}

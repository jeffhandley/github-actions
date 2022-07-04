using Octokit.GraphQL.Model;
using System.Collections.Generic;

namespace AreaPod.IssueTriage.Models;

internal struct IssueForTriage
{

    public int Number { get; init; }
    public string Title { get; init; }
    public IEnumerable<string> Labels { get; init; }
    public string? Milestone { get; init; }
    public CommentAuthorAssociation AuthorAssociation { get; init; }
}

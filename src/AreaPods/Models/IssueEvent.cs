#nullable disable warnings

namespace AreaPods.Models;

internal class IssueEvent
{
    public IssueAction? Action { get; init; }
    public string? Assignee { get; init; }
    public string? Label { get; init; }
    public string User { get; set; }
}

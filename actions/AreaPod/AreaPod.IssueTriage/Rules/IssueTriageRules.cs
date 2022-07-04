using AreaPod.IssueTriage.Models;

namespace AreaPod.IssueTriage.Rules;

internal static class IssueTriageRules
{
    internal static bool NeedsTriage(IssueForTriage issue) => !(
        issue.Closed ||
        issue.Milestone is not null ||
        issue.Labels.Contains("api-ready-for-review") ||
        issue.Labels.Contains("needs-author-action")
    );
}
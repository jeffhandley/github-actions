using AreaPod.IssueTriage.Models;
using System.Collections.Generic;

namespace AreaPod.IssueTriage.Rules;

internal static class IssueTriageRules
{
    public const string NeedsTriageLabel = "untriaged";

    static readonly List<string?> TriageCompletedLabels = new()
    {
        "api-ready-for-review",
        "needs-author-action"
    };

    static bool NeedsTriage(IssueForTriage issue) =>
        !issue.Closed &&
        issue.Milestone is null &&
        !issue.Labels.Exists(label => TriageCompletedLabels.Contains(label));

    static bool WasTriageNeededEvent(IssueEvent issueEvent) =>
        issueEvent.Action == IssueAction.Opened ||
        issueEvent.Action == IssueAction.Transferred;

    static bool WasTriageCompletedEvent(IssueEvent issueEvent) =>
        issueEvent.Action == IssueAction.Closed ||
        issueEvent.Action == IssueAction.Milestoned ||
        (
            issueEvent.Action == IssueAction.Labeled &&
            TriageCompletedLabels.Contains(issueEvent.Label)
        );

    //static bool WasFurtherTriageNeededEvent(IssueEvent issueEvent) =>
    //    issueEvent.Action == IssueAction.Reopened ||
    //    issueEvent.Action == IssueAction.Demilestoned ||
    //    (
    //        issueEvent.Action == IssueAction.Unlabeled &&
    //        TriageCompletedLabels.Contains(issueEvent.Label)
    //    );

    static bool HasNeedsTriageLabel(IssueForTriage issue) =>
        issue.Labels.Contains(NeedsTriageLabel);

    public static bool ShouldAddNeedsTriageLabel(IssueEvent issueEvent, IssueForTriage issue) =>
        WasTriageNeededEvent(issueEvent) &&
        NeedsTriage(issue) &&
        !HasNeedsTriageLabel(issue);

    public static bool ShouldRemoveTriageNeededLabel(IssueEvent issueEvent, IssueForTriage issue) =>
        WasTriageCompletedEvent(issueEvent) &&
        !NeedsTriage(issue) &&
        HasNeedsTriageLabel(issue);
}
#nullable disable warnings

namespace AreaPod.IssueTriage.Models;

internal class ProjectCardClassic
{
    public string Id { get; set; }
    public int ProjectNumber { get; init; }
    public string ProjectName { get; init; }
    public string ColumnName { get; init; }
    public bool IsArchived { get; init; }
}

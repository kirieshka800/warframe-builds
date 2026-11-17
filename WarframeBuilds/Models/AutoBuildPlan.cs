namespace WarframeBuilds.Models;

public sealed class AutoBuildPlan
{
    public Build Build { get; set; } = new();
    public string GoalLabel { get; set; } = "";
    public List<string> Notes { get; set; } = new();
    public Dictionary<int, string> Reasons { get; set; } = new();
}

namespace WarframeBuilds.Models;

public class BuildComment
{
    public int Id { get; set; }
    public int BuildId { get; set; }
    public Build? Build { get; set; }
    
    public string AuthorName { get; set; } = "Аноним";
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
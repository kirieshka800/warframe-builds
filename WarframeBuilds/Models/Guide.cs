using System.ComponentModel.DataAnnotations;
namespace WarframeBuilds.Models;
public class Guide
{
 public int Id { get; set; }
 [Required, StringLength(150)] public string Title { get; set; } = "";
 [Required, StringLength(1000)] public string Description { get; set; } = "";
 [Required, StringLength(40000)] public string Content { get; set; } = "";
 [Required, StringLength(60)] public string Category { get; set; } = "Основы";
 public string Author { get; set; } = "Tenno";
 public string? UserId { get; set; }
 public string ReadTime { get; set; } = "3 мин";
 public string IconClass { get; set; } = "bi-journal-text";
 public bool IsFeatured { get; set; }
 public string? ImageUrl { get; set; }
}

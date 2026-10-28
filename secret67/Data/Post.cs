using System.ComponentModel.DataAnnotations;

namespace secret67.Data;

public class Post
{
    public int Id { get; set; }
    
    public required string Text { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}
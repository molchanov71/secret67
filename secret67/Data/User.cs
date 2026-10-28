using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace secret67.Data;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    
    [MaxLength(32)]
    public required string Username { get; set; }
    
    [MaxLength(84)]
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Post> Posts { get; set; } = [];

    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();
}
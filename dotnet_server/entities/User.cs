public class User
{
  public int UserId { get; set; }  // Maps to user_id in the database
  public string Username { get; set; } = string.Empty;  // Maps to username in the database
  public string Email { get; set; } = string.Empty;  // Maps to email in the database
  public string PasswordHash { get; set; } = string.Empty;  // Maps to password_hash in the database
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Maps to created_at in the database

  public ICollection<Video>? Videos { get; set; }
}


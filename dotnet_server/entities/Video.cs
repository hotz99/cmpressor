public class Video
{
  public int VideoId { get; set; }  // Maps to video_id in the database
  public int? UserId { get; set; }  // Maps to user_id in the database
  public string Title { get; set; } = string.Empty;  // Maps to title in the database
  public string? Description { get; set; }  // Maps to description in the database
  public string S3Url { get; set; } = string.Empty;  // Maps to s3_url in the database
  public string? ThumbnailUrl { get; set; }  // Maps to thumbnail_url in the database
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Maps to created_at in the database
  public int Views { get; set; } = 0;  // Maps to views in the database
  public int Likes { get; set; } = 0;  // Maps to likes in the database

  public User? User { get; set; }
}

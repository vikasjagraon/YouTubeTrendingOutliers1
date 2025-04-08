namespace YouTubeTrendingOutliers.Models
{
    using System.Collections.Generic;
    public class YouTubeSearchResponse
    {
        public List<YouTubeSearchItem> Items { get; set; }
    }
    public class VideoWithScore
    {
        public VideoItem Video { get; set; }
        public double ZScore { get; set; }
    }
    public class YouTubeSearchItem
    {
        public Id Id { get; set; }
        public Snippet Snippet { get; set; }
    }

    public class Id
    {
        public string VideoId { get; set; }
    }

    
    public class YouTubeResponse
    {
        public List<VideoItem> Items  { get; set; }
    }

    public class VideoItem
    {
        public string Id { get; set; }
        public Snippet Snippet { get; set; }
        public Statistics Statistics { get; set; }
    }

    public class Snippet
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ChannelTitle { get; set; }
        public DateTime PublishedAt { get; set; }
        public ThumbnailDetails Thumbnails { get; set; }
    }

    public class Statistics
    {
        public string ViewCount { get; set; }
        public string LikeCount { get; set; }
        public string CommentCount { get; set; }
    }

    public class ThumbnailDetails
    {
        public Thumbnail Default { get; set; }
        public Thumbnail Medium { get; set; }
        public Thumbnail High { get; set; }
    }

    public class Thumbnail
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class UserLogin
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

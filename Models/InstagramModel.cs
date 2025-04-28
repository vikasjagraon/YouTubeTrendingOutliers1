namespace YouTubeTrendingOutliers.Models
{
    public class InstagramModel
    {

    }
    public class InstagramPost
    {
        public string Url { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class OutlierResult
    {
        public InstagramReel Reel { get; set; }
        public bool IsLikesOutlier { get; set; }
        public bool IsCommentsOutlier { get; set; }
    }
    public class InstagramReel
    {
        public string Url { get; set; }
        public string Topic { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime Timestamp { get; set; }
    }



}

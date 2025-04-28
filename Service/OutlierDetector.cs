using YouTubeTrendingOutliers.Models;

namespace YouTubeTrendingOutliers.Service
{
    public class OutlierResult
    {
        public InstagramReel Reel { get; set; }
        public bool IsLikesOutlier { get; set; }
        public bool IsCommentsOutlier { get; set; }
    }

    public static class OutlierDetector
    {
        public static List<OutlierResult> DetectOutliers(List<InstagramReel> reels)
        {
            var results = new List<OutlierResult>();

            var grouped = reels.GroupBy(r => new { r.Topic});

            foreach (var group in grouped)
            {
                var likeMean = group.Average(r => r.LikesCount);
                var likeStdDev = Math.Sqrt(group.Average(r => Math.Pow(r.LikesCount - likeMean, 2)));

                var commentMean = group.Average(r => r.CommentsCount);
                var commentStdDev = Math.Sqrt(group.Average(r => Math.Pow(r.CommentsCount - commentMean, 2)));

                foreach (var reel in group)
                {
                    var likeZ = likeStdDev == 0 ? 0 : Math.Abs((reel.LikesCount - likeMean) / likeStdDev);
                    var commentZ = commentStdDev == 0 ? 0 : Math.Abs((reel.CommentsCount - commentMean) / commentStdDev);

                    results.Add(new OutlierResult
                    {
                        Reel = reel,
                        IsLikesOutlier = likeZ > 2,
                        IsCommentsOutlier = commentZ > 2
                    });
                }
            }

            return results;
        }
    }

}

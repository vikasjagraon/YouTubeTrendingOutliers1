using Microsoft.AspNetCore.Mvc;
using YouTubeTrendingOutliers.Service;

namespace YouTubeTrendingOutliers.Controllers
{
    public class InstagramController : Controller
    {
        private const string ApifyToken = "apify_api_8VZn6fYbd58R9mo78eI1GT556Nuer73AFLl3";

        public async Task<ActionResult> Index()
        {
          
            var topics = new List<string> { "fashion"};

            var apify = new ApifyService("apify_api_8VZn6fYbd58R9mo78eI1GT556Nuer73AFLl3");
            var reels = await apify.GetReelsByHashtagTopicAsync(topics);

            var outliers = OutlierDetector.DetectOutliers(reels);

            return View(outliers);
        }
    }
}

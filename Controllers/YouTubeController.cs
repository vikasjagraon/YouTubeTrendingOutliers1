using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YouTubeTrendingOutliers.Models;

namespace YouTubeTrendingOutliers.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    
    [Authorize]
    public class YouTubeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private const string ApiKey = "AIzaSyBeOv4hDvDihLee0LbKU6GUy8zGJ9m1SzQ";

        public YouTubeController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Index() => View();
        
        [HttpPost]
        public async Task<IActionResult> Index(string topic, string regionCode)
        {
            if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(regionCode))
            {
                ViewBag.Error = "Please provide both topic and region code.";
                return View();
            }

            var client = _clientFactory.CreateClient();

            try
            {
                // Step 1: Get video IDs via Search API
                string searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(topic)}&type=video&regionCode={regionCode}&maxResults=50&key={ApiKey}";
                var searchResponse = await client.GetAsync(searchUrl);
                searchResponse.EnsureSuccessStatusCode();
                var searchJson = await searchResponse.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<YouTubeSearchResponse>(searchJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var videoIds = searchResult.Items.Select(i => i.Id.VideoId).ToList();
                string idList = string.Join(",", videoIds);

                // Step 2: Get statistics
                string statsUrl = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,statistics&id={idList}&key={ApiKey}";
                var statsResponse = await client.GetAsync(statsUrl);
                statsResponse.EnsureSuccessStatusCode();
                var statsJson = await statsResponse.Content.ReadAsStringAsync();
                var fullVideos = JsonSerializer.Deserialize<YouTubeResponse>(statsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Step 3: Compute mean + stddev
                var viewCounts = fullVideos.Items
                    .Select(v => long.TryParse(v.Statistics?.ViewCount, out var count) ? count : 0)
                    .ToList();

                double mean = viewCounts.Average();
                double stddev = Math.Sqrt(viewCounts.Average(v => Math.Pow(v - mean, 2)));

                // Step 4: Attach Z-Score and sort
                var enriched = fullVideos.Items
                    .Where(v => long.TryParse(v.Statistics?.ViewCount, out _))
                    .Select(v =>
                    {
                        long views = long.Parse(v.Statistics.ViewCount);
                        double zScore = stddev > 0 ? (views - mean) / stddev : 0;

                        return new VideoWithScore
                        {
                            Video = v,
                            ZScore = zScore
                        };
                    })
                    .OrderByDescending(x => x.ZScore)
                    .Take(50)
                    .ToList();

                ViewBag.Mean = mean;
                ViewBag.StdDev = stddev;
                ViewBag.Videos = enriched.Select(x => x.Video).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Something went wrong: " + ex.Message;
            }

            return View();
        }


    }

}

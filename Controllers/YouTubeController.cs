using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using YouTubeTrendingOutliers.Models;

namespace YouTubeTrendingOutliers.Controllers
{
    [Authorize]
    public class YouTubeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private const string ApiKey = "AIzaSyBeOv4hDvDihLee0LbKU6GUy8zGJ9m1SzQ"; // MASKED

        public YouTubeController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index(string topic, string? regionCode, string videoType = "videos")
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                ViewBag.Error = "Please provide a topic.";
                return View();
            }

            var client = _clientFactory.CreateClient();
            var videoIds = new HashSet<string>();
            DateTime startDate = new DateTime(2023, 4, 1);
            DateTime endDate = DateTime.UtcNow;
            TimeSpan interval = TimeSpan.FromDays(90);
            int maxTotal = 500; // Reduce to stay within quota

            try
            {
                while (startDate < endDate && videoIds.Count < maxTotal)
                {
                    var intervalEnd = startDate.Add(interval);
                    if (intervalEnd > endDate) intervalEnd = endDate;

                    string? nextPageToken = null;
                    int pageCount = 0;

                    do
                    {
                        var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(topic)}&type=video&maxResults=25&order=viewCount&publishedAfter={startDate:yyyy-MM-ddTHH:mm:ssZ}&publishedBefore={intervalEnd:yyyy-MM-ddTHH:mm:ssZ}&key={ApiKey}";

                        if (!string.IsNullOrWhiteSpace(regionCode))
                            searchUrl += $"&regionCode={regionCode}";
                        if (!string.IsNullOrWhiteSpace(nextPageToken))
                            searchUrl += $"&pageToken={nextPageToken}";

                        var response = await client.GetAsync(searchUrl);
                        if (!response.IsSuccessStatusCode) break;

                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<YouTubeSearchResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        var batchIds = result?.Items?.Where(i => i?.Id?.VideoId != null).Select(i => i.Id.VideoId);
                        if (batchIds != null)
                            foreach (var id in batchIds)
                                videoIds.Add(id);

                        nextPageToken = result?.NextPageToken;
                        pageCount++;
                    } while (!string.IsNullOrEmpty(nextPageToken) && pageCount < 3 && videoIds.Count < maxTotal);

                    startDate = intervalEnd;
                }

                if (!videoIds.Any())
                {
                    ViewBag.Error = "No videos found for the given topic.";
                    return View();
                }

                var allVideos = new List<YouTubeVideo>();
                foreach (var batch in videoIds.Chunk(50))
                {
                    var idList = string.Join(",", batch);
                    var statsUrl = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,statistics,contentDetails&id={idList}&key={ApiKey}";

                    var statsResponse = await client.GetAsync(statsUrl);
                    if (!statsResponse.IsSuccessStatusCode) continue;

                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    var statsResult = JsonSerializer.Deserialize<YouTubeResponse>(statsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (statsResult?.Items != null)
                        allVideos.AddRange(statsResult.Items);
                }

                var filteredVideos = allVideos
                    .Where(v => v?.Statistics?.ViewCount != null && TryParseIso8601Duration(v.ContentDetails?.Duration, out var d) &&
                        (videoType == "shorts" ? d.TotalSeconds <= 60 : d.TotalSeconds > 60))
                    .ToList();

                if (filteredVideos.Count < 4)
                {
                    ViewBag.Error = "Not enough data to calculate IQR outliers.";
                    return View();
                }

                var viewCounts = filteredVideos.Select(v => long.Parse(v.Statistics.ViewCount)).OrderBy(c => c).ToList();
                var Q1 = GetMedian(viewCounts.Take(viewCounts.Count / 2).ToList());
                var Q3 = GetMedian(viewCounts.Skip((viewCounts.Count + 1) / 2).ToList());
                var IQR = Q3 - Q1;
                var lowerBound = Q1 - 1.5 * IQR;
                var upperBound = Q3 + 1.5 * IQR;

                var enriched = filteredVideos.Select(v => new VideoWithScore
                {
                    Video = v,
                    ViewCount = long.Parse(v.Statistics.ViewCount)
                }).ToList();

                var outliers = enriched.Where(x => x.ViewCount < lowerBound || x.ViewCount > upperBound).OrderByDescending(x => x.ViewCount).ToList();

                if (outliers.Count < 20)
                {
                    var topFallback = enriched.OrderByDescending(x => x.ViewCount).Where(x => !outliers.Contains(x)).Take(20 - outliers.Count);
                    outliers.AddRange(topFallback);
                }

                ViewBag.Note = $"Found {outliers.Count} outliers using IQR.";
                ViewBag.Videos = outliers.Take(50).Select(x => x.Video).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
            }

            return View();
        }

        public static bool TryParseIso8601Duration(string? input, out TimeSpan result)
        {
            try
            {
                result = System.Xml.XmlConvert.ToTimeSpan(input);
                return true;
            }
            catch
            {
                result = TimeSpan.Zero;
                return false;
            }
        }

        public static double GetMedian(List<long> numbers)
        {
            if (numbers == null || !numbers.Any()) return 0;
            var sorted = numbers.OrderBy(n => n).ToList();
            int count = sorted.Count;
            return count % 2 == 0 ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0 : sorted[count / 2];
        }
    }
}

namespace YouTubeTrendingOutliers.Service
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using YouTubeTrendingOutliers.Models;

    public class ApifyService
    {
        private readonly string _apiToken;

        public ApifyService(string apiToken)
        {
            _apiToken = apiToken;
        }

        public async Task<List<InstagramReel>> GetReelsByHashtagTopicAsync(List<string> topics)
        {
            var reels = new List<InstagramReel>();
            using var client = new HttpClient();

            foreach (var topic in topics)
            {
                var requestBody = new
                {
                    hashtags = new[] { topic },
                    resultsLimit = 20,
                    scrapePosts = true
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var runResponse = await client.PostAsync(
                    $"https://api.apify.com/v2/acts/apify~instagram-scraper/runs?token={_apiToken}",
                    content
                );

                runResponse.EnsureSuccessStatusCode();
                var runJson = await runResponse.Content.ReadAsStringAsync();

                // Step 2: Extract dataset ID
                using var runDoc = JsonDocument.Parse(runJson);
                var datasetId = runDoc.RootElement
                    .GetProperty("data")
                    .GetProperty("defaultDatasetId")
                    .GetString();

                // Step 3: Fetch dataset items
                var datasetResponse = await client.GetAsync(
                    $"https://api.apify.com/v2/datasets/{datasetId}/items?token={_apiToken}"
                );

                datasetResponse.EnsureSuccessStatusCode();
                var datasetJson = await datasetResponse.Content.ReadAsStringAsync();

                using var datasetDoc = JsonDocument.Parse(datasetJson);
                var root = datasetDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.TryGetProperty("isVideo", out var isVideo) && isVideo.GetBoolean())
                        {
                            reels.Add(new InstagramReel
                            {
                                Url = item.GetProperty("url").GetString(),
                                LikesCount = item.GetProperty("likesCount").GetInt32(),
                                CommentsCount = item.GetProperty("commentsCount").GetInt32(),
                                Timestamp = DateTime.Parse(item.GetProperty("timestamp").GetString()),
                                Topic = topic
                            });
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Unexpected dataset format for topic {topic}");
                    Console.WriteLine(datasetJson);
                }
            }

            return reels;
        }
    }

}

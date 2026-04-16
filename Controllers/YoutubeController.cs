using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

[ApiController]
[Route("api/[controller]")]
public class YoutubeController : ControllerBase
{
    private const string ApiKey = "AIzaSyBuKKXQgyBE_GjcfCwKdl4YH_ROResrnUg";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(20);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public YoutubeController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] string[] videoIds)
    {
        var normalizedVideoIds = videoIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedVideoIds.Count == 0)
            return BadRequest("No video IDs provided.");

        var cachedResults = new Dictionary<string, YoutubeStatistics>(StringComparer.OrdinalIgnoreCase);
        var uncachedVideoIds = new List<string>();

        foreach (var id in normalizedVideoIds)
        {
            if (_cache.TryGetValue<YoutubeStatistics>(GetCacheKey(id), out var stats) && stats is not null)
            {
                cachedResults[id] = stats;
            }
            else
            {
                uncachedVideoIds.Add(id);
            }
        }

        if (uncachedVideoIds.Count > 0)
        {
            try
            {
                var fetchedStats = await FetchYoutubeStats(uncachedVideoIds);
                foreach (var pair in fetchedStats)
                {
                    cachedResults[pair.Key] = pair.Value;
                    _cache.Set(GetCacheKey(pair.Key), pair.Value, CacheTtl);
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return StatusCode(429, new { error = "Quota exceeded. Try again later." });
            }
        }

        var results = normalizedVideoIds
            .Where(id => cachedResults.ContainsKey(id))
            .Select(id => new
            {
                Song = id,
                Views = cachedResults[id].ViewCount,
                Likes = cachedResults[id].LikeCount
            })
            .ToList();

        return Ok(results);
    }

    private async Task<Dictionary<string, YoutubeStatistics>> FetchYoutubeStats(List<string> videoIds)
    {
        var client = _httpClientFactory.CreateClient();
        var output = new Dictionary<string, YoutubeStatistics>(StringComparer.OrdinalIgnoreCase);

        foreach (var batch in Chunk(videoIds, 50))
        {
            var joinedIds = string.Join(',', batch);
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=statistics&id={joinedIds}&key={ApiKey}";

            var response = await client.GetFromJsonAsync<YoutubeApiResponse>(url);
            if (response?.Items is null)
                continue;

            foreach (var item in response.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                    continue;

                output[item.Id] = new YoutubeStatistics
                {
                    ViewCount = item.Statistics.ViewCount,
                    LikeCount = item.Statistics.LikeCount
                };
            }
        }

        return output;
    }

    private static IEnumerable<List<string>> Chunk(List<string> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
        {
            yield return source.Skip(i).Take(size).ToList();
        }
    }

    private static string GetCacheKey(string videoId) => $"youtube:stats:{videoId}";
}

public class YoutubeApiResponse
{
    public YoutubeItem[] Items { get; set; } = Array.Empty<YoutubeItem>();
}

public class YoutubeItem
{
    public string Id { get; set; } = string.Empty;
    public YoutubeStatistics Statistics { get; set; } = new YoutubeStatistics();
}

public class YoutubeStatistics
{
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;

[ApiController]
[Route("api/[controller]")]
public class YoutubeController : ControllerBase
{
    private readonly HttpClient _client;

    public YoutubeController()
    {
        _client = new HttpClient();
    }
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] string[] videoIds)
    {
        if (videoIds.Length == 0)
            return BadRequest("No video IDs provided.");

        var results = new List<object>();

        try
        {
            foreach (var id in videoIds)
            {
                //var apiKey = "AIzaSyAerYqGIIm4AD-kv595jSs5Vpz0Nanscbs";
                var apiKey = "AIzaSyBuKKXQgyBE_GjcfCwKdl4YH_ROResrnUg";
                var url = $"https://www.googleapis.com/youtube/v3/videos?part=statistics&id={id}&key={apiKey}";

                var response = await _client.GetFromJsonAsync<YoutubeApiResponse>(url);

                if (response != null && response.Items.Any())
                {
                    results.Add(new
                    {
                        Song = id,
                        Views = response.Items[0].Statistics.ViewCount,
                        Likes = response.Items[0].Statistics.LikeCount
                    });
                }
            }
            return Ok(results);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403"))
        {
            return StatusCode(429, new { error = "Quota exceeded. Try again later." });
        }
    }

    //[HttpGet("trends")]
    //public async Task<IActionResult> GetTrends([FromQuery] string[] videoIds)
    //{
    //    if (videoIds.Length == 0)
    //        return BadRequest("No video IDs provided.");

    //    var results = new List<object>();
    //    foreach (var id in videoIds)
    //    {
    //        var apiKey = "AIzaSyAerYqGIIm4AD-kv595jSs5Vpz0Nanscbs";
    //        var url = $"https://www.googleapis.com/youtube/v3/videos?part=statistics&id={id}&key={apiKey}";
    //        var response = await _client.GetFromJsonAsync<YoutubeApiResponse>(url);

    //        if (response != null && response.Items.Any())
    //        {
    //            results.Add(new
    //            {
    //                Song = id, // you can map ID -> song name if you have a dictionary
    //                Views = response.Items[0].Statistics.ViewCount,
    //                Likes = response.Items[0].Statistics.LikeCount
    //            });
    //        }
    //    }

    //    return Ok(results);
    //}

}

// Models for YouTube API response
public class YoutubeApiResponse
{
    public YoutubeItem[] Items { get; set; } = Array.Empty<YoutubeItem>();
}

public class YoutubeItem
{
    public YoutubeStatistics Statistics { get; set; } = new YoutubeStatistics();
}

public class YoutubeStatistics
{
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
}

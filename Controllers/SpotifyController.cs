//using Microsoft.AspNetCore.Mvc;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;

//[ApiController]
//[Route("api/[controller]")]
//public class SpotifyController : ControllerBase
//{
//    private readonly HttpClient _client;

//    public SpotifyController()
//    {
//        _client = new HttpClient();
//    }

//    [HttpGet("track")]
//    public async Task<IActionResult> GetTrack(string trackId = "<SPOTIFY_TRACK_ID>", string token = "<YOUR_SPOTIFY_OAUTH_TOKEN>")
//    {
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//        var url = $"https://api.spotify.com/v1/tracks/{trackId}";
//        var res = await _client.GetFromJsonAsync<SpotifyTrackResponse>(url);

//        if (res == null) return BadRequest();

//        return Ok(new
//        {
//            res.Name,
//            res.Popularity,
//            res.Album.Name,
//            Artists = res.Artists.Select(a => a.Name)
//        });
//    }
//}

//// Spotify models
//public class SpotifyTrackResponse
//{
//    public string Name { get; set; } = "";
//    public int Popularity { get; set; }
//    public SpotifyAlbum Album { get; set; } = new SpotifyAlbum();
//    public SpotifyArtist[] Artists { get; set; } = Array.Empty<SpotifyArtist>();
//}

//public class SpotifyAlbum { public string Name { get; set; } = ""; }
//public class SpotifyArtist { public string Name { get; set; } = ""; }

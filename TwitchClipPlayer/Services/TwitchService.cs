using TwitchClipPlayer.Models;
using TwitchClipPlayer.Config;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace TwitchClipPlayer.Services;

public interface ITwitchService
{
    Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate);
}

public class TwitchService(IHttpClientFactory httpClientFactory, TwitchConfig config) : ITwitchService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly string _clientId = config.ClientId ?? throw new ArgumentNullException(nameof(config.ClientId));
    private readonly string _clientSecret = config.ClientSecret ?? throw new ArgumentNullException(nameof(config.ClientSecret));
    private readonly string _broadcasterId = config.BroadcasterId ?? throw new ArgumentNullException(nameof(config.BroadcasterId));

    // Method to get access token from Twitch API
    public async Task<string> GetAccessToken()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        }));

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error fetching access token: {response.StatusCode} - {errorText}");
            throw new Exception($"Error fetching access token: {response.StatusCode} - {errorText}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            Console.WriteLine("Failed to retrieve access token.");
            throw new Exception("Failed to retrieve access token.");
        }

        // Log the access token for debugging purposes
        Console.WriteLine($"Access Token: {tokenResponse.AccessToken}");

        return tokenResponse.AccessToken;
    }

    // Method to fetch clips from Twitch API
    public async Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate)
    {
        var accessToken = await GetAccessToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var url = BuildClipsUrl(startDate, endDate);
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error fetching clips: {response.StatusCode} - {errorText}");
            throw new Exception($"Error fetching clips: {response.StatusCode} - {errorText}");
        }

        var clipsResponse = await DeserializeClipsResponse(response);

        return ProcessClips(clipsResponse);
    }

    // Method to build the URL for fetching clips
    public string BuildClipsUrl(DateTime startDate, DateTime endDate)
    {
        return $"https://api.twitch.tv/helix/clips?broadcaster_id={_broadcasterId}&first=20&started_at={startDate:O}&ended_at={endDate:O}";
    }

    // Method to deserialize the clips response
    private async Task<ClipsResponse> DeserializeClipsResponse(HttpResponseMessage response)
    {
        var clipsResponse = await response.Content.ReadFromJsonAsync<ClipsResponse>();

        if (clipsResponse == null)
        {
            throw new Exception("Failed to deserialize clips response.");
        }

        return clipsResponse;
    }

    // Method to process the clips data
    public List<Clip> ProcessClips(ClipsResponse clipsResponse)
    {
        if (clipsResponse?.Data == null)
        {
            Console.WriteLine("No clips data found.");
            return new List<Clip>();
        }

        var randomizedClips = RandomizeClipsOrder(clipsResponse.Data);
        var filteredClips = FilterRecentClips(randomizedClips);

        return filteredClips;
    }

    // Method to randomize the order of clips
    public List<Clip> RandomizeClipsOrder(List<Clip> clips)
    {
        var random = new Random();
        return clips.OrderBy(x => random.Next()).ToList();
    }

    // Method to filter out clips created within the last 3 weeks
    public List<Clip> FilterRecentClips(List<Clip> clips)
    {
        var threeWeeksAgo = DateTime.UtcNow.AddDays(-21);
        return clips.Where(clip => clip.CreatedAt < threeWeeksAgo).ToList();
    }
}
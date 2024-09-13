using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TwitchClipViewer.Models;
using TwitchClipViewer.Config;

public interface ITwitchService
{
    Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate);
}

public class TwitchService : ITwitchService
{

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _broadcasterId;

    public TwitchService(IHttpClientFactory httpClientFactory, TwitchConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _clientId = config.ClientId ?? throw new ArgumentNullException(nameof(config.ClientId));
        _clientSecret = config.ClientSecret ?? throw new ArgumentNullException(nameof(config.ClientSecret));
        _broadcasterId = config.BroadcasterId ?? throw new ArgumentNullException(nameof(config.BroadcasterId));
    }

    private async Task<string> GetAccessToken()
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

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw Token Response: {responseContent}"); // Log the raw response content

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

    public async Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate)
    {
        var accessToken = await GetAccessToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        // Log the authorization header for debugging purposes
        Console.WriteLine($"Authorization Header: Bearer {accessToken}");

        var url = $"https://api.twitch.tv/helix/clips?broadcaster_id={_broadcasterId}&first=20&started_at={startDate:O}&ended_at={endDate:O}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error fetching clips: {response.StatusCode} - {errorText}");
            throw new Exception($"Error fetching clips: {response.StatusCode} - {errorText}");
        }

        var clipsResponse = await response.Content.ReadFromJsonAsync<ClipsResponse>();

        if (clipsResponse?.Data == null)
        {
            Console.WriteLine("No clips data found.");
            return new List<Clip>();
        }

        var random = new Random();
        // Randomize the order of clips
        clipsResponse.Data = clipsResponse.Data.OrderBy(x => random.Next()).ToList();

        // Filter out clips created within the last 3 weeks
        var threeWeeksAgo = DateTime.UtcNow.AddDays(-21);
        clipsResponse.Data = clipsResponse.Data.Where(clip => clip.CreatedAt < threeWeeksAgo).ToList();

        return clipsResponse.Data;
    }
}
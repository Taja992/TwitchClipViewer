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
    Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate, string broadcasterId);
    Task<string> GetChannelIdByName(string channelName); // New method
}

public class TwitchService : ITwitchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<TwitchService> _logger;
    private string _accessToken = string.Empty;
    private DateTime _accessTokenExpiry;

    public TwitchService(IHttpClientFactory httpClientFactory, TwitchConfig config, ILogger<TwitchService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clientId = config.ClientId ?? throw new ArgumentNullException(nameof(config.ClientId));
        _clientSecret = config.ClientSecret ?? throw new ArgumentNullException(nameof(config.ClientSecret));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // Method to get access token from Twitch API
    public async Task<string> GetAccessToken()
    {
        // Log the current state of the token and its expiry time
        _logger.LogInformation("Current Access Token: {AccessToken}", _accessToken);
        _logger.LogInformation("Current Access Token Expiry: {ExpiryTime}", _accessTokenExpiry.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrEmpty(_accessToken) && _accessTokenExpiry > DateTime.UtcNow)
        {
            _logger.LogInformation("Using cached access token.");
            return _accessToken;
        }

        _logger.LogInformation("Fetching new access token.");
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
            _logger.LogError("Error fetching access token: {StatusCode} - {ErrorText}", response.StatusCode, errorText);
            throw new Exception($"Error fetching access token: {response.StatusCode} - {errorText}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            _logger.LogError("Failed to retrieve access token.");
            throw new Exception("Failed to retrieve access token.");
        }

        // Log the access token for debugging purposes
        _logger.LogInformation("New Access Token: {AccessToken}", tokenResponse.AccessToken);

        _accessToken = tokenResponse.AccessToken;
        _accessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 0);

        // Log the new expiration time in a human-readable format
        _logger.LogInformation("New Access Token Expiry: {ExpiryTime}", _accessTokenExpiry.ToString("yyyy-MM-dd HH:mm:ss"));

        return _accessToken;
    }

    // Method to fetch clips from Twitch API
    public async Task<List<Clip>> FetchClips(DateTime startDate, DateTime endDate, string broadcasterId)
    {
        var accessToken = await GetAccessToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var url = BuildClipsUrl(startDate, endDate, broadcasterId);
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error fetching clips: {StatusCode} - {errorText}", response.StatusCode, errorText);
            throw new Exception($"Error fetching clips: {response.StatusCode} - {errorText}");
        }

        var clipsResponse = await DeserializeClipsResponse(response);

        return ProcessClips(clipsResponse);
    }

    // Method to build the URL for fetching clips
    public string BuildClipsUrl(DateTime startDate, DateTime endDate, string broadcasterId)
    {
        return $"https://api.twitch.tv/helix/clips?broadcaster_id={broadcasterId}&first=20&started_at={startDate:O}&ended_at={endDate:O}";
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
            _logger.LogInformation("No clips data found.");
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

    // New method to get channel ID by channel name
    public async Task<string> GetChannelIdByName(string channelName)
    {
        var accessToken = await GetAccessToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        client.DefaultRequestHeaders.Add("Client-Id", _clientId);

        var response = await client.GetAsync($"https://api.twitch.tv/helix/users?login={channelName}");

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error fetching channel ID: {StatusCode} - {errorText}", response.StatusCode, errorText);
            throw new Exception($"Error fetching channel ID: {response.StatusCode} - {errorText}");
        }

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();

        if (userResponse == null || userResponse.Data == null || userResponse.Data.Count == 0)
        {
            _logger.LogError("Failed to retrieve channel ID.");
            throw new Exception("Failed to retrieve channel ID.");
        }

        var channelId = userResponse.Data[0].Id;

        if (string.IsNullOrEmpty(channelId))
        {
            _logger.LogError("Channel ID is null or empty.");
            throw new Exception("Channel ID is null or empty.");
        }

        return channelId;
    }
}
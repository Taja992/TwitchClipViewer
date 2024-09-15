using TwitchClipPlayer.Config;
using TwitchClipPlayer.Models;
using TwitchClipPlayer.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;


namespace TwitchClipPlayerTests;


public class TwitchClipPlayerTests
{

    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly TwitchService _twitchService;

    public TwitchClipPlayerTests()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var config = new TwitchConfig
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            BroadcasterId = "test-broadcaster-id"
        };
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);
        _twitchService = new TwitchService(httpClientFactoryMock.Object, config);
    }

    [Fact]
    public async Task GetAccessToken_ShouldReturnAccessToken_WhenResponseIsSuccessful()
    {
        // Arrange
        var tokenResponse = new TokenResponse { AccessToken = "test-access-token" };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(tokenResponse)
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);
        
        // Act
        var accessToken = await _twitchService.GetAccessToken();
        
        // Assert
        
        Assert.Equal("test-access-token", accessToken);
    }

    [Fact]
    public async Task GetAccessToken_ShouldThrowException_WhenResponseIsUnsuccessful()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _twitchService.GetAccessToken());
    }

    [Fact]
    public async Task FetchClips_ShouldReturnClips_WhenResponseIsSuccessful()
    {
        //Arrange
        var tokenResponse = new TokenResponse { AccessToken = "test-access-token" };
        var tokenResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(tokenResponse)
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsoluteUri.Contains("oauth2/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(tokenResponseMessage);

        var clipsResponse = new ClipsResponse
        {
            Data = new List<Clip> { new Clip { Id = "test-clip-id", CreatedAt = DateTime.UtcNow.AddDays(-30) } }
        };
        var clipsResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(clipsResponse)
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsoluteUri.Contains("helix/clips")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(clipsResponseMessage);
        
        // Act
        var clips = await _twitchService.FetchClips(DateTime.UtcNow.AddDays(-60), DateTime.UtcNow);
        
        // Assert
        Assert.Single(clips);
        Assert.Equal("test-clip-id", clips[0].Id);
    }

    [Fact]
    public void BuildClipsUrl_ShouldReturnCorrectUrl()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-60);
        var endDate = DateTime.UtcNow;
        
        // Act
        var url = _twitchService.BuildClipsUrl(startDate, endDate);
        
        // Assert
        Assert.Contains("broadcaster_id=test-broadcaster-id", url);
        Assert.Contains($"started_at={startDate:O}", url);
        Assert.Contains($"ended_at={endDate:O}", url);
    }

    [Fact]
    public void ProcessClips_ShouldReturnEmptyList_WhenNoClipsData()
    {
        // Arrange
        var clipsResponse = new ClipsResponse() { Data = null };
        
        // Act

        var clips = _twitchService.ProcessClips(clipsResponse);
        
        //Assert
        Assert.Empty(clips);
    }

    [Fact]
    public void RandomizeClipsOrder_ShouldRandomizeOrder()
    {
        // Arrange
        var clips = new List<Clip>
        {
            new Clip() { Id = "clip1" },
            new Clip() { Id = "clip2" },
            new Clip() { Id = "Clip3" }
        };
        
        // Act
        var randomizedClips = _twitchService.RandomizeClipsOrder(clips);
        
        // Assert
        Assert.NotEqual(clips, randomizedClips);
    }

    [Fact]
    public void FilterRecentClips_ShouldFilterOutRecentClips()
    {
        // Arrange
        var clips = new List<Clip>
        {
            new Clip { Id = "clip1", CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Clip { Id = "clip2", CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };
        
        // Act
        var filteredClips = _twitchService.FilterRecentClips(clips);
        
        // Assert
        Assert.Single(filteredClips);
        Assert.Equal("clip2", filteredClips[0].Id);
    }
    
        [Fact]
        public void ProcessClips_ReturnsExpectedClips()
        {
            // Arrange
            var clipsResponse = new ClipsResponse
            {
                Data = new List<Clip>
                {
                    new Clip { Id = "1", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new Clip { Id = "2", CreatedAt = DateTime.UtcNow.AddDays(-40) },
                    new Clip { Id = "3", CreatedAt = DateTime.UtcNow.AddDays(-10) } // This clip should be filtered out
                }
            };
            
            // Act
            var result = _twitchService.ProcessClips(clipsResponse);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Clip>>(result);
            Assert.Equal(2, result.Count); // Only 2 clips should be returned
            Assert.DoesNotContain(result,
                clip => clip.CreatedAt >= DateTime.UtcNow.AddDays(-21)); // Ensure no clips are within the last 3 weeks
        }
    }
    
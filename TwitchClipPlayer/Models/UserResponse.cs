namespace TwitchClipPlayer.Models
{
    public class UserResponse
    {
        public List<User>? Data { get; set; }
    }

    public class User
    {
        public string? Id { get; set; }
        public string? Login { get; set; }
        public string? DisplayName { get; set; }
    }
}
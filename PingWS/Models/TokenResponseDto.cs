namespace PingWS.Models
{
    public class TokenResponseDto
    {
        public required Guid Id { get; set; }
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}

namespace PingWS.Models
{
  public class LoginResponseDto
  {
    public required Guid Id { get; set; }
    public required string UserName { get; set; }
    public string Role {  get; set; } = string.Empty;
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
  }
}

namespace SecureCompany.Application.DTOs
{
    public class RefreshTokenResult
    {
        public DateTime ExpiresAt { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
namespace SecureCompany.Application.DTOs
{
    public class AuthResponse
    {
        public DateTime ExpiresAt { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
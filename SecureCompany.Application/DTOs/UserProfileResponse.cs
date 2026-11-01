namespace SecureCompany.Application.DTOs
{
    public class UserProfileResponse
    {
        public string? UserId { get; set; }

        public string? Email { get; set; }

        public string? UserName { get; set; }

        public string? FullName { get; set; }

        public string? Department { get; set; }

        public bool IsAuthenticated { get; set; }


        public IList<string> Permissions { get; set; } = new List<string>();
        public IList<string> Roles { get; set; } = new List<string>();
        public IDictionary<string, string[]> Claims { get; set; }
            = new Dictionary<string, string[]>();
    }
}
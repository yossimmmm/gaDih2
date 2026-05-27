namespace Models
{
    public enum UserRole
    {
        User = 0,
        Manager = 1,
        Admin = 2
    }

    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.User;
    }
}

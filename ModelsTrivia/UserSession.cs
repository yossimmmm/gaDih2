namespace Models
{
    // Lightweight global holder for the current user ID.
    // This is a convenience helper, not a full authentication system.
    public static class UserSession
    {
        // The currently logged-in user ID, if one is set.
        public static int? CurrentUserID { get; set; }
    }
}

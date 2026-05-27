using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

public sealed class UsersDomainService
{
    // טעינת פרופיל מלא לפי מזהה משתמש.
    public async Task<User?> GetByIdAsync(int userId)
    {
        var userDb = new UserDB();
        return await userDb.GetByIdAsync(userId);
    }

    // עדכון פרופיל משתמש מחובר.
    public async Task<(bool Ok, string Message)> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var (usernameValid, usernameError) = ValidationHelper.ValidateUsername(req.Username);
        if (!usernameValid)
            return (false, usernameError);

        var (fullNameValid, fullNameError) = ValidationHelper.ValidateFullName(req.FullName);
        if (!fullNameValid)
            return (false, fullNameError);

        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Please enter a valid email address.");

        var userDb = new UserDB();
        var current = await userDb.GetByIdAsync(userId);
        if (current is null)
            return (false, "User not found.");

        var normalizedUsername = ValidationHelper.SanitizeInput(req.Username, 50);
        var normalizedFullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var normalizedEmail = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();

        var existingUsername = await userDb.GetByUsernameAsync(normalizedUsername);
        if (existingUsername is not null && existingUsername.UserID != userId)
            return (false, "Username already taken.");

        var existingEmail = await userDb.GetByEmailAsync(normalizedEmail);
        if (existingEmail is not null && existingEmail.UserID != userId)
            return (false, "Email already registered.");

        var ok = await userDb.UpdateProfileAsync(userId, normalizedUsername, normalizedFullName, normalizedEmail);
        return ok ? (true, "Profile updated.") : (false, "Profile update failed.");
    }

    // עדכון סיסמה למשתמש מחובר.
    public async Task<(bool Ok, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var userDb = new UserDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        if (!PasswordHelper.Verify(req.CurrentPassword ?? "", user.PasswordHash))
            return (false, "Current password is incorrect.");

        var (valid, error) = ValidationHelper.ValidatePassword(req.NewPassword);
        if (!valid)
            return (false, error);

        var updated = await userDb.UpdatePasswordAsync(userId, PasswordHelper.Hash(req.NewPassword.Trim()));
        return updated ? (true, "Password updated.") : (false, "Password update failed.");
    }

    // שליפת כל המשתמשים (לאדמין).
    public async Task<List<User>> GetAllUsersAsync()
    {
        var userDb = new UserDB();
        return await userDb.GetAllUsersAsync();
    }

    // שינוי role (אדמין).
    public async Task<(bool Ok, string Message)> UpdateRoleAsync(int userId, string role)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        var userDb = new UserDB();
        var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
        return ok ? (true, "Role updated.") : (false, "User role update failed.");
    }

    // עדכון משתמש מלא (אדמין).
    public async Task<(bool Ok, string Message)> UpdateUserByAdminAsync(int userId, AdminUserUpdateRequest req)
    {
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Invalid email.");
        if (!Enum.TryParse<UserRole>(req.Role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        var userDb = new UserDB();
        var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);
        return ok ? (true, "User updated.") : (false, "User update failed.");
    }

    // מחיקת משתמש (אדמין).
    public async Task<(bool Ok, string Message)> DeleteUserAsync(int userId)
    {
        var userDb = new UserDB();
        var ok = await userDb.DeleteUserAsync(userId);
        return ok ? (true, "User deleted.") : (false, "Delete failed.");
    }
}

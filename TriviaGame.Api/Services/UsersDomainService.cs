using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// שירות הדומיין של המשתמשים.
// כאן נמצאת הלוגיקה העסקית, וה־controller רק מעביר בקשות לכאן.
public sealed class UsersDomainService
{
    // מחזיר משתמש לפי מזהה.
    // ה־API שולח userId, והשירות שואל את שכבת ה־DBL מי המשתמש הזה.
    public async Task<User?> GetByIdAsync(int userId)
    {
        // בכל קריאה נוצרת מעטפת גישה לנתונים.
        var userDb = new UserDB();
        return await userDb.GetByIdAsync(userId);
    }

    // מעדכן פרופיל משתמש.
    // ה־API שולח גם userId וגם את הערכים החדשים: username, full name, email.
    public async Task<(bool Ok, string Message)> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        // בודקים שהשם תקין מבחינת אורך ותווים.
        var (usernameValid, usernameError) = ValidationHelper.ValidateUsername(req.Username);
        if (!usernameValid)
            return (false, usernameError);

        // אחר כך בודקים את השם המלא.
        var (fullNameValid, fullNameError) = ValidationHelper.ValidateFullName(req.FullName);
        if (!fullNameValid)
            return (false, fullNameError);

        // אימייל חייב להיות בפורמט תקין.
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Please enter a valid email address.");

        // טוענים את המשתמש הקיים כדי לוודא שהוא באמת קיים.
        var userDb = new UserDB();
        var current = await userDb.GetByIdAsync(userId);
        if (current is null)
            return (false, "User not found.");

        // מנקים את הערכים לפני שמכניסים אותם למסד.
        var normalizedUsername = ValidationHelper.SanitizeInput(req.Username, 50);
        var normalizedFullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var normalizedEmail = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();

        // בודקים אם username כבר תפוס על ידי מישהו אחר.
        var existingUsername = await userDb.GetByUsernameAsync(normalizedUsername);
        if (existingUsername is not null && existingUsername.UserID != userId)
            return (false, "Username already taken.");

        // בודקים אם האימייל כבר רשום אצל משתמש אחר.
        var existingEmail = await userDb.GetByEmailAsync(normalizedEmail);
        if (existingEmail is not null && existingEmail.UserID != userId)
            return (false, "Email already registered.");

        // אם הכול תקין, שומרים את הפרופיל המעודכן.
        var ok = await userDb.UpdateProfileAsync(userId, normalizedUsername, normalizedFullName, normalizedEmail);
        return ok ? (true, "Profile updated.") : (false, "Profile update failed.");
    }

    // משנה סיסמה של המשתמש המחובר.
    // הלקוח שולח את הסיסמה הישנה ואת הסיסמה החדשה.
    public async Task<(bool Ok, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        // טוענים את המשתמש כדי לעבוד מול הסיסמה השמורה.
        var userDb = new UserDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        // אם הסיסמה הישנה לא תואמת, עוצרים כאן.
        if (!PasswordHelper.Verify(req.CurrentPassword ?? "", user.PasswordHash))
            return (false, "Current password is incorrect.");

        // גם הסיסמה החדשה חייבת לעבור את כללי הוולידציה.
        var (valid, error) = ValidationHelper.ValidatePassword(req.NewPassword);
        if (!valid)
            return (false, error);

        // שומרים רק hash חדש במקום הטקסט המקורי.
        var updated = await userDb.UpdatePasswordAsync(userId, PasswordHelper.Hash(req.NewPassword.Trim()));
        return updated ? (true, "Password updated.") : (false, "Password update failed.");
    }

    // מחזיר את כל המשתמשים.
    // זה מיועד למסכי אדמין ולא למסך רגיל של שחקן.
    public async Task<List<User>> GetAllUsersAsync()
    {
        // אין כאן לוגיקת עסקית מיוחדת, רק קריאה ישירה לשכבת הנתונים.
        var userDb = new UserDB();
        return await userDb.GetAllUsersAsync();
    }

    // משנה role של משתמש.
    // לדוגמה: User, Manager, Admin.
    public async Task<(bool Ok, string Message)> UpdateRoleAsync(int userId, string role)
    {
        // ממירים את הטקסט לאנום אמיתי.
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        // שומרים את השינוי במסד דרך ה־DBL.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
        return ok ? (true, "Role updated.") : (false, "User role update failed.");
    }

    // מעדכן משתמש מלא מצד אדמין.
    // כאן אפשר לשנות שם, אימייל, fullname ותפקיד.
    public async Task<(bool Ok, string Message)> UpdateUserByAdminAsync(int userId, AdminUserUpdateRequest req)
    {
        // גם האימייל צריך להיות תקין לפני ששומרים אותו.
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Invalid email.");

        // ממירים את התפקיד לטיפוס enum כדי למנוע ערכים לא חוקיים.
        if (!Enum.TryParse<UserRole>(req.Role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        // ה־DBL עושה את שמירת הערכים בפועל.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);
        return ok ? (true, "User updated.") : (false, "User update failed.");
    }

    // מוחק משתמש.
    // גם זה נעשה דרך ה־DBL בלבד.
    public async Task<(bool Ok, string Message)> DeleteUserAsync(int userId)
    {
        var userDb = new UserDB();
        var ok = await userDb.DeleteUserAsync(userId);
        return ok ? (true, "User deleted.") : (false, "Delete failed.");
    }
}

using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// שירותי הדומיין של המשתמשים.
// כאן נמצאת הלוגיקה העסקית, וה-controller רק מעביר בקשות לשירות.
public sealed class UsersDomainService
{
    // מחזיר משתמש לפי מזהה.
    public async Task<User?> GetByIdAsync(int userId)
    {
        // בכל קריאה נוצרת מעטפת גישה ל-DB.
        var userDb = new UserDB();

        // מחזירים null אם לא נמצא משתמש, וה-controller מחליט איך לענות ללקוח.
        return await userDb.GetByIdAsync(userId);
    }

    // מעדכן פרופיל משתמש.
    public async Task<(bool Ok, string Message)> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        // כל עדכון פרופיל מתחיל מבדיקות קלט כדי לא להכניס נתונים לא תקינים למסד.
        // בודקים שהשם תקין לפני שממשיכים.
        var (usernameValid, usernameError) = ValidationHelper.ValidateUsername(req.Username);
        if (!usernameValid)
            return (false, usernameError);

        // אחר כך בודקים גם את השם המלא.
        var (fullNameValid, fullNameError) = ValidationHelper.ValidateFullName(req.FullName);
        if (!fullNameValid)
            return (false, fullNameError);

        // אימייל חייב להיות תקין.
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Please enter a valid email address.");

        // טוענים את המשתמש הקיים כדי לוודא שהוא באמת קיים.
        var userDb = new UserDB();
        var current = await userDb.GetByIdAsync(userId);
        if (current is null)
            return (false, "User not found.");

        // מנרמלים את הערכים לפני שמכניסים אותם למסד.
        // SanitizeInput גם מקצרת לפי אורך מקסימלי וגם מחליפה תווים בעייתיים לתצוגה.
        var normalizedUsername = ValidationHelper.SanitizeInput(req.Username, 50);
        var normalizedFullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var normalizedEmail = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();

        // בודקים אם username כבר תפוס על ידי משתמש אחר.
        var existingUsername = await userDb.GetByUsernameAsync(normalizedUsername);
        if (existingUsername is not null && existingUsername.UserID != userId)
            return (false, "Username already taken.");

        // בודקים אם האימייל כבר משויך למשתמש אחר.
        var existingEmail = await userDb.GetByEmailAsync(normalizedEmail);
        if (existingEmail is not null && existingEmail.UserID != userId)
            return (false, "Email already registered.");

        // אם הכול תקין, שומרים את הפרופיל המעודכן.
        var ok = await userDb.UpdateProfileAsync(userId, normalizedUsername, normalizedFullName, normalizedEmail);

        // מחזירים tuple פשוט כדי שה-controller יתורגם בקלות ל-HTTP response.
        return ok ? (true, "Profile updated.") : (false, "Profile update failed.");
    }

    // משנה סיסמה של משתמש מחובר.
    public async Task<(bool Ok, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        // טוענים את המשתמש כדי לבדוק מול הסיסמה השמורה.
        var userDb = new UserDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        // אם הסיסמה הישנה לא נכונה, עוצרים כאן.
        if (!PasswordHelper.Verify(req.CurrentPassword ?? "", user.PasswordHash))
            return (false, "Current password is incorrect.");

        // גם הסיסמה החדשה חייבת לעבור את הוולידציה.
        var (valid, error) = ValidationHelper.ValidatePassword(req.NewPassword);
        if (!valid)
            return (false, error);

        // שומרים רק hash חדש ולא את הטקסט הגלוי.
        var updated = await userDb.UpdatePasswordAsync(userId, PasswordHelper.Hash(req.NewPassword.Trim()));

        // אם update נכשל, כנראה שהמשתמש לא נמצא או שהמסד לא עדכן שורה.
        return updated ? (true, "Password updated.") : (false, "Password update failed.");
    }

    // מחזיר את כל המשתמשים.
    public async Task<List<User>> GetAllUsersAsync()
    {
        // אין כאן לוגיקת עסקית, רק קריאה ישירה ל-DB.
        // השימוש העיקרי הוא מסך אדמין שמציג רשימת משתמשים.
        var userDb = new UserDB();
        return await userDb.GetAllUsersAsync();
    }

    // משנה role של משתמש.
    public async Task<(bool Ok, string Message)> UpdateRoleAsync(int userId, string role)
    {
        // ממירים את המחרוזת ל-enum.
        // כך מונעים שמירה של תפקידים שלא קיימים בקוד.
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return (false, "Role must be User or Admin.");

        // מעבירים את השינוי לשכבת הנתונים.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
        return ok ? (true, "Role updated.") : (false, "User role update failed.");
    }

    // מעדכן משתמש ברמת אדמין.
    public async Task<(bool Ok, string Message)> UpdateUserByAdminAsync(int userId, AdminUserUpdateRequest req)
    {
        // עדכון אדמין עדיין חייב לעבור ולידציה, גם אם מי שמבצע אותו הוא אדמין.
        // גם האימייל חייב להיות תקין.
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Invalid email.");

        // ממירים את ה-role ל-enum כדי למנוע ערכים לא חוקיים.
        if (!Enum.TryParse<UserRole>(req.Role, true, out var parsedRole))
            return (false, "Role must be User or Admin.");

        // ה-DBL מבצע את הכתיבה בפועל.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);

        // השירות מחזיר הודעה קצרה שה-controller מחזיר ללקוח.
        return ok ? (true, "User updated.") : (false, "User update failed.");
    }

    // מוחק משתמש.
    public async Task<(bool Ok, string Message)> DeleteUserAsync(int userId)
    {
        // מחיקה עוברת דרך ה-DBL כדי שכל קשרי המסד יטופלו במקום אחד.
        var userDb = new UserDB();
        var ok = await userDb.DeleteUserAsync(userId);
        return ok ? (true, "User deleted.") : (false, "Delete failed.");
    }
}

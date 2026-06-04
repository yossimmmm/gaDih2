using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// השירות הזה מרכז את כל הלוגיקה של משתמשים:
// קריאה לפרופיל, עדכון פרטים, שינוי סיסמה, וניהול משתמשים מצד אדמין.
// ה-controller לא מדבר ישירות מול בסיס הנתונים, אלא מעביר את הבקשה לכאן.
public sealed class UsersDomainService
{
    // מחזיר משתמש לפי מזהה.
    // ה-API שולח כאן userId, והשירות שואל את שכבת ה-DBL מי המשתמש הזה.
    public async Task<User?> GetByIdAsync(int userId)
    {
        // בכל קריאה נוצר מופע של שכבת הגישה לנתונים.
        // זה החיבור הישיר לטבלאות ולשאילתות של המשתמשים.
        var userDb = new UserDB();
        return await userDb.GetByIdAsync(userId);
    }

    // עדכון פרופיל משתמש.
    // ה-API שולח גם את ה-userId וגם את הנתונים החדשים:
    // username, full name, email.
    public async Task<(bool Ok, string Message)> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        // קודם בודקים שהשם תקין מבחינת אורך ותווים.
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

        // טוענים את המשתמש הקיים כדי לוודא שהוא בכלל קיים.
        var userDb = new UserDB();
        var current = await userDb.GetByIdAsync(userId);
        if (current is null)
            return (false, "User not found.");

        // מנקים את הערכים לפני שמכניסים אותם למסד:
        // חיתוך אורך, trim, והקטנת email לאותיות קטנות.
        var normalizedUsername = ValidationHelper.SanitizeInput(req.Username, 50);
        var normalizedFullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var normalizedEmail = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();

        // בודקים אם שם המשתמש כבר תפוס על ידי מישהו אחר.
        var existingUsername = await userDb.GetByUsernameAsync(normalizedUsername);
        if (existingUsername is not null && existingUsername.UserID != userId)
            return (false, "Username already taken.");

        // בודקים אם האימייל כבר רשום על משתמש אחר.
        var existingEmail = await userDb.GetByEmailAsync(normalizedEmail);
        if (existingEmail is not null && existingEmail.UserID != userId)
            return (false, "Email already registered.");

        // אם כל הבדיקות עברו, שומרים את הפרופיל החדש במסד.
        var ok = await userDb.UpdateProfileAsync(userId, normalizedUsername, normalizedFullName, normalizedEmail);
        return ok ? (true, "Profile updated.") : (false, "Profile update failed.");
    }

    // שינוי סיסמה של המשתמש המחובר.
    // הלקוח שולח את הסיסמה הישנה ואת הסיסמה החדשה.
    public async Task<(bool Ok, string Message)> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        // טוענים את המשתמש כדי לגשת לסיסמה המוצפנת הקיימת.
        var userDb = new UserDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        // אם הסיסמה הישנה לא תואמת, עוצרים כאן.
        // זה מונע שינוי סיסמה בלי לדעת את הסיסמה הנוכחית.
        if (!PasswordHelper.Verify(req.CurrentPassword ?? "", user.PasswordHash))
            return (false, "Current password is incorrect.");

        // גם הסיסמה החדשה חייבת לעבור כללי תקינות בסיסיים.
        var (valid, error) = ValidationHelper.ValidatePassword(req.NewPassword);
        if (!valid)
            return (false, error);

        // אחרי הבדיקה, שומרים hash חדש במקום הטקסט עצמו.
        var updated = await userDb.UpdatePasswordAsync(userId, PasswordHelper.Hash(req.NewPassword.Trim()));
        return updated ? (true, "Password updated.") : (false, "Password update failed.");
    }

    // מחזיר את כל המשתמשים.
    // זה מיועד למסכי אדמין ולא למסך רגיל של שחקן.
    public async Task<List<User>> GetAllUsersAsync()
    {
        // הקריאה עצמה עדיין עוברת דרך DBL.
        // כאן אין לוגיקת business מיוחדת, רק שליפה מלאה.
        var userDb = new UserDB();
        return await userDb.GetAllUsersAsync();
    }

    // שינוי תפקיד של משתמש.
    // למשל User, Manager, Admin.
    public async Task<(bool Ok, string Message)> UpdateRoleAsync(int userId, string role)
    {
        // קודם ממירים את הטקסט לאנום אמיתי.
        // אם הטקסט לא תואם לאף ערך, מחזירים שגיאה.
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        // שמירה למסד הנתונים דרך שכבת ה-DBL.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
        return ok ? (true, "Role updated.") : (false, "User role update failed.");
    }

    // עדכון משתמש מלא מצד אדמין.
    // כאן האדמין יכול לשנות שם, אימייל, שם מלא ותפקיד.
    public async Task<(bool Ok, string Message)> UpdateUserByAdminAsync(int userId, AdminUserUpdateRequest req)
    {
        // גם כאן בודקים את האימייל לפני ששומרים.
        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Invalid email.");

        // ממירים את התפקיד לטיפוס אנום כדי להימנע מערכים חופשיים.
        if (!Enum.TryParse<UserRole>(req.Role, true, out var parsedRole))
            return (false, "Role must be User, Manager, or Admin.");

        // ה-DBL מקבל כאן את כל הערכים המנוקים ושומר אותם.
        var userDb = new UserDB();
        var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);
        return ok ? (true, "User updated.") : (false, "User update failed.");
    }

    // מחיקת משתמש.
    // גם זה מסלול אדמין בלבד.
    public async Task<(bool Ok, string Message)> DeleteUserAsync(int userId)
    {
        // הקריאה עוברת לשכבת הנתונים, ושם נמחקת הרשומה.
        var userDb = new UserDB();
        var ok = await userDb.DeleteUserAsync(userId);
        return ok ? (true, "User deleted.") : (false, "Delete failed.");
    }
}

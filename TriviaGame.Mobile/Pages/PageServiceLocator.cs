using Microsoft.Extensions.DependencyInjection;

namespace TriviaGame.Mobile.Pages;

// עוזר קטן לדפי MAUI.
// Shell יוצר את הדפים דרך XAML, ולכן constructor רגיל עם פרמטרים של DI לא תמיד נקרא.
// המחלקה הזאת מאפשרת לדף לשלוף service מתוך ה-container שנבנה ב-MauiProgram.cs.
internal static class PageServiceLocator
{
    // T הוא סוג ה-service שהדף מבקש, לדוגמה TriviaApiClient.
    // where T : notnull אומר שהמתודה לא אמורה להחזיר null.
    public static T Get<T>() where T : notnull
    {
        // Application.Current.Handler.MauiContext.Services הוא ה-service provider של MAUI.
        // משם הדפים מקבלים TriviaApiClient, ApiEndpointResolver ו-MobileSessionState.
        // Application.Current הוא מופע App הפעיל.
        // Handler מחבר את קוד MAUI למימוש הפלטפורמה, ו-MauiContext מכיל את ה-DI container.
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI service provider is unavailable.");

        // GetRequiredService מחזיר את ה-service הרשום.
        // אם שכחנו לרשום אותו ב-MauiProgram, תיזרק שגיאה ברורה במקום לקבל null.
        return services.GetRequiredService<T>();
    }
}

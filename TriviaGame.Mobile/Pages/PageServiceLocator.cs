using Microsoft.Extensions.DependencyInjection;

namespace TriviaGame.Mobile.Pages;

// עוזר קטן לדפי MAUI.
// Shell יוצר את הדפים דרך XAML, ולכן constructor רגיל עם פרמטרים של DI לא תמיד נקרא.
// המחלקה הזאת מאפשרת לדף לשלוף service מתוך ה-container שנבנה ב-MauiProgram.cs.
internal static class PageServiceLocator
{
    public static T Get<T>() where T : notnull
    {
        // Application.Current.Handler.MauiContext.Services הוא ה-service provider של MAUI.
        // משם הדפים מקבלים TriviaApiClient, ApiEndpointResolver ו-MobileSessionState.
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI service provider is unavailable.");

        return services.GetRequiredService<T>();
    }
}

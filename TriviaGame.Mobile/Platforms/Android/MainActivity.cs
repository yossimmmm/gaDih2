using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TriviaGame.Mobile;

// Activity ראשי של Android.
// כאן Android פותח את אפליקציית MAUI ומעביר אליה את השליטה.
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}

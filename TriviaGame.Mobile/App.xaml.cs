using Microsoft.Extensions.DependencyInjection;

namespace TriviaGame.Mobile;

public partial class App : Application
{
	public App()
	{
		// טעינת משאבי XAML גלובליים של האפליקציה
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// יצירת חלון ראשי שמארח את AppShell
		return new Window(new AppShell());
	}
}

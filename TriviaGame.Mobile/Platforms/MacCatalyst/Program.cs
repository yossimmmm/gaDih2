using ObjCRuntime;
using UIKit;

namespace TriviaGame.Mobile;

public class Program
{
	// זהו נקודת הכניסה הראשית של האפליקציה.
	static void Main(string[] args)
	{
		// אם רוצים להשתמש במחלקת Application Delegate אחרת מ-"AppDelegate",
		// אפשר להגדיר אותה כאן.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}

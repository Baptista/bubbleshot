using BubbleShot.Views;

namespace BubbleShot;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
		Routing.RegisterRoute(nameof(HighScoresPage), typeof(HighScoresPage));
	}
}

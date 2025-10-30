using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BubbleShot.Services;
using BubbleShot.ViewModels;
using BubbleShot.Views;

namespace BubbleShot;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register services
		builder.Services.AddSingleton<DatabaseService>();

		// Register ViewModels
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<GameViewModel>();
		builder.Services.AddSingleton<HighScoresViewModel>();

		// Register Views
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<GamePage>();
		builder.Services.AddSingleton<HighScoresPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

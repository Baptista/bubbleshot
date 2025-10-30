using System.Windows.Input;
using BubbleShot.Views;

namespace BubbleShot.ViewModels;

public class MainViewModel : BaseViewModel
{
    public ICommand StartGameCommand { get; }
    public ICommand ViewHighScoresCommand { get; }

    public MainViewModel()
    {
        StartGameCommand = new Command(async () => await StartGame());
        ViewHighScoresCommand = new Command(async () => await ViewHighScores());
    }

    private async Task StartGame()
    {
        await Shell.Current.GoToAsync(nameof(GamePage));
    }

    private async Task ViewHighScores()
    {
        await Shell.Current.GoToAsync(nameof(HighScoresPage));
    }
}

using BubbleShot.ViewModels;

namespace BubbleShot.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _viewModel;

    public GamePage(GameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize game when canvas is ready
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            var width = (float)GameCanvas.Width;
            var height = (float)GameCanvas.Height;

            if (width > 0 && height > 0)
            {
                _viewModel.InitializeGame(width, height);
                GameCanvas.GameEngine = _viewModel.GameEngine;
                GameCanvas.StartGameLoop();
            }
        });

        // Monitor game state
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            _viewModel.CheckGameState();
            return !_viewModel.GameEngine.GameState.IsGameOver &&
                   !_viewModel.GameEngine.GameState.IsLevelComplete;
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameCanvas.StopGameLoop();
    }
}

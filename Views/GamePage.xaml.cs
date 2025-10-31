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

        // Set the game engine - it will initialize on first paint with actual canvas size
        GameCanvas.GameEngine = _viewModel.GameEngine;
        GameCanvas.StartGameLoop();

        // Monitor game state - only stop timer on game over, not level complete
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            _viewModel.CheckGameState();
            return !_viewModel.GameEngine.GameState.IsGameOver;
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameCanvas.StopGameLoop();
    }
}

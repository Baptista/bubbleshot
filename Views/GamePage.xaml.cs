using BubbleShot.ViewModels;

namespace BubbleShot.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private bool _isMonitoring;

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

        // Monitor game state continuously
        // Prevent multiple timers from being created
        if (!_isMonitoring)
        {
            _isMonitoring = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                if (!_isMonitoring)
                    return false;

                _viewModel.CheckGameState();

                // Keep timer running even after game over/level complete
                // so that restart/next level detection works correctly
                return true;
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameCanvas.StopGameLoop();
        _isMonitoring = false;
    }
}

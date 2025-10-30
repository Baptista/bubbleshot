using BubbleShot.ViewModels;

namespace BubbleShot.Views;

public partial class HighScoresPage : ContentPage
{
    private readonly HighScoresViewModel _viewModel;

    public HighScoresPage(HighScoresViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadHighScores();
    }
}

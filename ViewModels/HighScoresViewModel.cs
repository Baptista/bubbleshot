using System.Collections.ObjectModel;
using System.Windows.Input;
using BubbleShot.Models;
using BubbleShot.Services;

namespace BubbleShot.ViewModels;

public class HighScoresViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<HighScore> HighScores { get; }
    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public HighScoresViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        HighScores = new ObservableCollection<HighScore>();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        RefreshCommand = new Command(async () => await LoadHighScores());
    }

    public async Task LoadHighScores()
    {
        IsLoading = true;

        try
        {
            var scores = await _databaseService.GetHighScoresAsync();
            HighScores.Clear();

            foreach (var score in scores)
            {
                HighScores.Add(score);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

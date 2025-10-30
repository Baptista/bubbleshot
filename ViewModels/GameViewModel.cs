using System.Windows.Input;
using BubbleShot.Models;
using BubbleShot.Services;

namespace BubbleShot.ViewModels;

public class GameViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private GameEngine _gameEngine;
    private int _currentLevel;

    public GameEngine GameEngine => _gameEngine;

    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand NextLevelCommand { get; }
    public ICommand BackToMenuCommand { get; }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public GameViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _gameEngine = new GameEngine();
        _currentLevel = 1;

        PauseCommand = new Command(Pause, () => !_gameEngine.GameState.IsPaused);
        ResumeCommand = new Command(Resume, () => _gameEngine.GameState.IsPaused);
        RestartCommand = new Command(Restart);
        NextLevelCommand = new Command(NextLevel, () => _gameEngine.GameState.IsLevelComplete);
        BackToMenuCommand = new Command(async () => await BackToMenu());
    }

    public void InitializeGame(float width, float height, int level = 1)
    {
        _currentLevel = level;
        _gameEngine.InitializeGame(width, height, level);
        UpdateStatus();
    }

    private void Pause()
    {
        _gameEngine.GameState.IsPaused = true;
        UpdateStatus();
        ((Command)PauseCommand).ChangeCanExecute();
        ((Command)ResumeCommand).ChangeCanExecute();
    }

    private void Resume()
    {
        _gameEngine.GameState.IsPaused = false;
        UpdateStatus();
        ((Command)PauseCommand).ChangeCanExecute();
        ((Command)ResumeCommand).ChangeCanExecute();
    }

    private void Restart()
    {
        _gameEngine.InitializeGame(_gameEngine.ShooterPosition.X * 2,
                                   _gameEngine.ShooterPosition.Y + 100,
                                   _currentLevel);
        UpdateStatus();
    }

    private void NextLevel()
    {
        _currentLevel++;
        _gameEngine.InitializeGame(_gameEngine.ShooterPosition.X * 2,
                                   _gameEngine.ShooterPosition.Y + 100,
                                   _currentLevel);
        UpdateStatus();
        ((Command)NextLevelCommand).ChangeCanExecute();
    }

    private async Task BackToMenu()
    {
        // Save high score if applicable
        if (_gameEngine.GameState.Score > 0)
        {
            bool isHighScore = await _databaseService.IsHighScore(_gameEngine.GameState.Score);
            if (isHighScore)
            {
                await SaveHighScore();
            }
        }

        await Shell.Current.GoToAsync("..");
    }

    private async Task SaveHighScore()
    {
        var highScore = new HighScore
        {
            PlayerName = "Player",
            Score = _gameEngine.GameState.Score,
            Level = _gameEngine.GameState.CurrentLevel
        };

        await _databaseService.SaveHighScoreAsync(highScore);
    }

    public void CheckGameState()
    {
        if (_gameEngine.GameState.IsGameOver)
        {
            StatusMessage = "Game Over! Tap Back to return to menu.";
            _ = SaveHighScoreIfNeeded();
        }
        else if (_gameEngine.GameState.IsLevelComplete)
        {
            StatusMessage = "Level Complete! Tap Next Level to continue.";
            ((Command)NextLevelCommand).ChangeCanExecute();
        }
    }

    private async Task SaveHighScoreIfNeeded()
    {
        if (_gameEngine.GameState.Score > 0)
        {
            bool isHighScore = await _databaseService.IsHighScore(_gameEngine.GameState.Score);
            if (isHighScore)
            {
                await SaveHighScore();
            }
        }
    }

    private void UpdateStatus()
    {
        if (_gameEngine.GameState.IsPaused)
        {
            StatusMessage = "Game Paused";
        }
        else if (_gameEngine.GameState.IsGameOver)
        {
            StatusMessage = "Game Over!";
        }
        else if (_gameEngine.GameState.IsLevelComplete)
        {
            StatusMessage = "Level Complete!";
        }
        else
        {
            StatusMessage = "Playing...";
        }
    }
}

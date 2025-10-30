using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BubbleShot.Models;

public class GameState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _currentLevel;
    private int _score;
    private int _bubblesRemaining;
    private bool _isGameOver;
    private bool _isLevelComplete;
    private bool _isPaused;
    private int _combo;

    public int CurrentLevel
    {
        get => _currentLevel;
        set
        {
            if (_currentLevel != value)
            {
                _currentLevel = value;
                OnPropertyChanged();
            }
        }
    }

    public int Score
    {
        get => _score;
        set
        {
            if (_score != value)
            {
                _score = value;
                OnPropertyChanged();
            }
        }
    }

    public int BubblesRemaining
    {
        get => _bubblesRemaining;
        set
        {
            if (_bubblesRemaining != value)
            {
                _bubblesRemaining = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsGameOver
    {
        get => _isGameOver;
        set
        {
            if (_isGameOver != value)
            {
                _isGameOver = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLevelComplete
    {
        get => _isLevelComplete;
        set
        {
            if (_isLevelComplete != value)
            {
                _isLevelComplete = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                OnPropertyChanged();
            }
        }
    }

    public int Combo
    {
        get => _combo;
        set
        {
            if (_combo != value)
            {
                _combo = value;
                OnPropertyChanged();
            }
        }
    }

    public GameState()
    {
        CurrentLevel = 1;
        Score = 0;
        BubblesRemaining = 0;
        IsGameOver = false;
        IsLevelComplete = false;
        IsPaused = false;
        Combo = 0;
    }

    public void AddScore(int bubblesPopped)
    {
        int baseScore = bubblesPopped * 10;
        int comboBonus = Combo * 5;
        Score += baseScore + comboBonus;

        if (bubblesPopped > 0)
        {
            Combo++;
        }
        else
        {
            Combo = 0;
        }
    }

    public void ResetCombo()
    {
        Combo = 0;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

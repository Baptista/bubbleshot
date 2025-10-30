namespace BubbleShot.Models;

public class GameState
{
    public int CurrentLevel { get; set; }
    public int Score { get; set; }
    public int BubblesRemaining { get; set; }
    public bool IsGameOver { get; set; }
    public bool IsLevelComplete { get; set; }
    public bool IsPaused { get; set; }
    public int Combo { get; set; }

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
}

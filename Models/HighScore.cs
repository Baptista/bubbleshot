using SQLite;

namespace BubbleShot.Models;

[Table("HighScores")]
public class HighScore
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string PlayerName { get; set; } = "Player";

    public int Score { get; set; }

    public int Level { get; set; }

    public DateTime DateAchieved { get; set; }
}

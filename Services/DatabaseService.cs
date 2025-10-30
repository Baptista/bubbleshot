using SQLite;
using BubbleShot.Models;

namespace BubbleShot.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task Init()
    {
        if (_database != null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "bubbleshot.db");
        _database = new SQLiteAsyncConnection(dbPath);
        await _database.CreateTableAsync<HighScore>();
    }

    public async Task<List<HighScore>> GetHighScoresAsync()
    {
        await Init();
        return await _database!.Table<HighScore>()
            .OrderByDescending(h => h.Score)
            .Take(10)
            .ToListAsync();
    }

    public async Task<int> SaveHighScoreAsync(HighScore highScore)
    {
        await Init();
        highScore.DateAchieved = DateTime.Now;
        return await _database!.InsertAsync(highScore);
    }

    public async Task<bool> IsHighScore(int score)
    {
        await Init();
        var highScores = await GetHighScoresAsync();
        return highScores.Count < 10 || score > highScores.LastOrDefault()?.Score;
    }
}

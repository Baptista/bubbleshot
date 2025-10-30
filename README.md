# BubbleShot - Bubble Shooter Game

A classic bubble shooter game built with .NET MAUI, SkiaSharp, and SQLite.

## Features

- **Classic Gameplay**: Shoot bubbles to match 3 or more of the same color
- **Multiple Levels**: Progressive difficulty with more colors and rows
- **Combo System**: Chain matches for bonus points
- **High Scores**: SQLite database tracks top 10 scores
- **Smooth Graphics**: SkiaSharp rendering for fluid animations
- **Touch Controls**: Intuitive aiming and shooting mechanics

## Technology Stack

- **.NET MAUI**: Cross-platform framework (Android, iOS, Windows)
- **SkiaSharp**: 2D graphics rendering
- **SQLite**: Local database for high scores
- **MVVM Pattern**: Clean architecture

## Game Mechanics

1. **Aim**: Touch and drag to aim your bubble
2. **Shoot**: Release to shoot the bubble
3. **Match**: Connect 3+ bubbles of the same color to pop them
4. **Win**: Clear all bubbles to complete the level
5. **Lose**: If bubbles reach the bottom, game over

## Project Structure

```
BubbleShot/
├── Models/           # Data models (Bubble, GameState, HighScore)
├── Views/            # XAML pages (MainPage, GamePage, HighScoresPage)
├── ViewModels/       # MVVM view models
├── Services/         # Game engine and database service
├── Controls/         # Custom SkiaSharp game canvas
├── Resources/        # Styles, colors, fonts, images
└── Platforms/        # Platform-specific code
```

## Build & Run

### Requirements
- .NET 8.0 SDK
- Android SDK (for Android builds)
- Visual Studio 2022 or JetBrains Rider

### Build Commands
```bash
# Restore packages
dotnet restore

# Build for Android
dotnet build -t:Run -f net8.0-android34.0
```

## How to Play

1. Launch the app
2. Tap "START GAME" to begin
3. Aim by touching the screen
4. Release to shoot bubbles
5. Match 3+ colors to pop them
6. Clear all bubbles to advance to the next level

## License

MIT License - Feel free to use and modify!

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using BubbleShot.Services;

namespace BubbleShot.Controls;

public class GameCanvasView : SKCanvasView
{
    private GameEngine? _gameEngine;
    private DateTime _lastUpdate;
    private bool _isRunning;
    private bool _isInitialized;

    public GameEngine? GameEngine
    {
        get => _gameEngine;
        set
        {
            _gameEngine = value;
            _isInitialized = false;  // Reset initialization flag when engine changes
            InvalidateSurface();
        }
    }

    public GameCanvasView()
    {
        _lastUpdate = DateTime.Now;
        EnableTouchEvents = true;
        Touch += OnTouch;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        var info = e.Info;

        canvas.Clear(SKColors.Black);

        if (_gameEngine == null)
            return;

        // Initialize game with actual canvas size on first paint
        if (!_isInitialized && info.Width > 0 && info.Height > 0)
        {
            _gameEngine.InitializeGame(info.Width, info.Height, _gameEngine.GameState.CurrentLevel);
            _isInitialized = true;
        }

        // Draw background gradient
        DrawBackground(canvas, info);

        // Draw bubbles
        DrawBubbles(canvas);

        // Draw current bubble (being shot)
        DrawCurrentBubble(canvas);

        // Draw next bubble preview
        DrawNextBubble(canvas);

        // Draw aiming line
        DrawAimingLine(canvas);

        // Draw shooter
        DrawShooter(canvas);

        // Draw UI
        DrawUI(canvas, info);
    }

    private void DrawBackground(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(info.Width / 2, 0),
                new SKPoint(info.Width / 2, info.Height),
                new[] { new SKColor(20, 20, 50), new SKColor(10, 10, 30) },
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, 0, info.Width, info.Height, paint);
    }

    private void DrawBubbles(SKCanvas canvas)
    {
        if (_gameEngine == null)
            return;

        foreach (var bubble in _gameEngine.Bubbles)
        {
            if (bubble.IsPopping)
            {
                // Pop animation
                float scale = 1 - bubble.PopAnimationProgress;
                float alpha = 1 - bubble.PopAnimationProgress;

                using var paint = new SKPaint
                {
                    Color = bubble.GetSKColor().WithAlpha((byte)(alpha * 255)),
                    IsAntialias = true
                };

                canvas.DrawCircle(bubble.Position, bubble.Radius * scale, paint);

                // Outer ring
                using var ringPaint = new SKPaint
                {
                    Color = SKColors.White.WithAlpha((byte)(alpha * 128)),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3,
                    IsAntialias = true
                };
                canvas.DrawCircle(bubble.Position, bubble.Radius * scale * 1.2f, ringPaint);
            }
            else
            {
                DrawBubble(canvas, bubble.Position, bubble.Radius, bubble.GetSKColor());
            }
        }
    }

    private void DrawBubble(SKCanvas canvas, SKPoint position, float radius, SKColor color)
    {
        // Main bubble
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(position.X - radius * 0.3f, position.Y - radius * 0.3f),
                radius * 1.2f,
                new[] { color.WithAlpha(255), color.WithAlpha(180) },
                SKShaderTileMode.Clamp)
        };
        canvas.DrawCircle(position, radius, paint);

        // Highlight
        using var highlightPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(100),
            IsAntialias = true
        };
        canvas.DrawCircle(new SKPoint(position.X - radius * 0.3f, position.Y - radius * 0.3f),
                         radius * 0.4f, highlightPaint);

        // Border
        using var borderPaint = new SKPaint
        {
            Color = color.WithAlpha(200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawCircle(position, radius, borderPaint);
    }

    private void DrawCurrentBubble(SKCanvas canvas)
    {
        if (_gameEngine?.CurrentBubble == null)
            return;

        var position = _gameEngine.GetShootingBubblePosition();
        DrawBubble(canvas, position, _gameEngine.CurrentBubble.Radius,
                  _gameEngine.CurrentBubble.GetSKColor());
    }

    private void DrawNextBubble(SKCanvas canvas)
    {
        if (_gameEngine?.NextBubble == null)
            return;

        var bubble = _gameEngine.NextBubble;
        DrawBubble(canvas, bubble.Position, bubble.Radius, bubble.GetSKColor());

        // Label
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 16,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
        canvas.DrawText("Next", bubble.Position.X, bubble.Position.Y + bubble.Radius + 20, textPaint);
    }

    private void DrawAimingLine(SKCanvas canvas)
    {
        if (_gameEngine == null || _gameEngine.IsShootingInProgress)
            return;

        var start = _gameEngine.ShooterPosition;
        var direction = _gameEngine.AimDirection;

        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(100),
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 10f, 5f }, 0)
        };

        // Draw dotted line
        var end = new SKPoint(
            start.X + direction.X * 500,
            start.Y + direction.Y * 500
        );
        canvas.DrawLine(start, end, paint);
    }

    private void DrawShooter(SKCanvas canvas)
    {
        if (_gameEngine == null)
            return;

        var position = _gameEngine.ShooterPosition;

        // Base
        using var basePaint = new SKPaint
        {
            Color = new SKColor(80, 80, 100),
            IsAntialias = true
        };
        canvas.DrawCircle(position, 25, basePaint);

        // Highlight
        using var highlightPaint = new SKPaint
        {
            Color = new SKColor(120, 120, 140),
            IsAntialias = true
        };
        canvas.DrawCircle(new SKPoint(position.X - 5, position.Y - 5), 10, highlightPaint);
    }

    private void DrawUI(SKCanvas canvas, SKImageInfo info)
    {
        if (_gameEngine?.GameState == null)
            return;

        var state = _gameEngine.GameState;

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24,
            IsAntialias = true,
            FakeBoldText = true
        };

        // Score
        canvas.DrawText($"Score: {state.Score}", 20, 40, textPaint);

        // Level
        canvas.DrawText($"Level: {state.CurrentLevel}", 20, 70, textPaint);

        // Combo
        if (state.Combo > 1)
        {
            using var comboPaint = new SKPaint
            {
                Color = SKColors.Yellow,
                TextSize = 28,
                IsAntialias = true,
                FakeBoldText = true
            };
            canvas.DrawText($"Combo x{state.Combo}!", info.Width / 2 - 60, 40, comboPaint);
        }

        // Game Over / Level Complete
        if (state.IsGameOver)
        {
            DrawCenteredMessage(canvas, info, "GAME OVER", SKColors.Red);
        }
        else if (state.IsLevelComplete)
        {
            DrawCenteredMessage(canvas, info, "LEVEL COMPLETE!", SKColors.Green);
        }
        else if (state.IsPaused)
        {
            DrawCenteredMessage(canvas, info, "PAUSED", SKColors.Yellow);
        }
    }

    private void DrawCenteredMessage(SKCanvas canvas, SKImageInfo info, string message, SKColor color)
    {
        // Background
        using var bgPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(180)
        };
        canvas.DrawRect(0, info.Height / 2 - 60, info.Width, 120, bgPaint);

        // Text
        using var textPaint = new SKPaint
        {
            Color = color,
            TextSize = 48,
            IsAntialias = true,
            FakeBoldText = true,
            TextAlign = SKTextAlign.Center
        };
        canvas.DrawText(message, info.Width / 2, info.Height / 2 + 15, textPaint);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (_gameEngine == null || _gameEngine.GameState.IsGameOver ||
            _gameEngine.GameState.IsPaused || _gameEngine.IsShootingInProgress)
        {
            e.Handled = true;
            return;
        }

        if (e.ActionType == SKTouchAction.Moved || e.ActionType == SKTouchAction.Pressed)
        {
            // Calculate aim direction
            var shooterPos = _gameEngine.ShooterPosition;
            var direction = new SKPoint(
                e.Location.X - shooterPos.X,
                e.Location.Y - shooterPos.Y
            );

            // Only allow shooting upwards
            if (direction.Y < 0)
            {
                _gameEngine.AimDirection = direction;
                InvalidateSurface();
            }
        }
        else if (e.ActionType == SKTouchAction.Released)
        {
            _gameEngine.StartShooting(_gameEngine.AimDirection);
        }

        e.Handled = true;
    }

    public void StartGameLoop()
    {
        _isRunning = true;
        _lastUpdate = DateTime.Now;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (!_isRunning)
                return false;

            var now = DateTime.Now;
            var deltaTime = (float)(now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            _gameEngine?.Update(deltaTime);
            InvalidateSurface();

            return true;
        });
    }

    public void StopGameLoop()
    {
        _isRunning = false;
    }
}

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

    // Reusable paint objects to reduce garbage collection
    private readonly SKPaint _bubblePaint = new SKPaint { IsAntialias = true };
    private readonly SKPaint _highlightPaint = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(100) };
    private readonly SKPaint _borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
    private readonly SKPaint _ringPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
    private readonly SKPaint _textPaint = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 24, FakeBoldText = true };
    private readonly SKPaint _comboPaint = new SKPaint { IsAntialias = true, Color = SKColors.Yellow, TextSize = 28, FakeBoldText = true };

    public GameEngine? GameEngine
    {
        get => _gameEngine;
        set
        {
            _gameEngine = value;
            // Don't reset initialization flag - canvas size stays the same
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

                _bubblePaint.Color = bubble.GetSKColor().WithAlpha((byte)(alpha * 255));
                canvas.DrawCircle(bubble.Position, bubble.Radius * scale, _bubblePaint);

                // Outer ring
                _ringPaint.Color = SKColors.White.WithAlpha((byte)(alpha * 128));
                canvas.DrawCircle(bubble.Position, bubble.Radius * scale * 1.2f, _ringPaint);
            }
            else
            {
                DrawBubble(canvas, bubble.Position, bubble.Radius, bubble.GetSKColor());
            }
        }
    }

    private void DrawBubble(SKCanvas canvas, SKPoint position, float radius, SKColor color)
    {
        // Main bubble with gradient shader
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(position.X - radius * 0.3f, position.Y - radius * 0.3f),
            radius * 1.2f,
            new[] { color.WithAlpha(255), color.WithAlpha(180) },
            SKShaderTileMode.Clamp);

        _bubblePaint.Color = color;
        _bubblePaint.Shader = shader;
        canvas.DrawCircle(position, radius, _bubblePaint);
        _bubblePaint.Shader = null; // Clear shader for next use

        // Highlight
        canvas.DrawCircle(new SKPoint(position.X - radius * 0.3f, position.Y - radius * 0.3f),
                         radius * 0.4f, _highlightPaint);

        // Border
        _borderPaint.Color = color.WithAlpha(200);
        canvas.DrawCircle(position, radius, _borderPaint);
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
        var canvasWidth = _gameEngine.CanvasWidth;
        var canvasHeight = _gameEngine.CanvasHeight;
        var bubbleRadius = _gameEngine.CurrentBubble?.Radius ?? 30;

        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(120),
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 10f, 5f }, 0)
        };

        // Normalize direction
        float length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length == 0) return;

        var normalizedDir = new SKPoint(direction.X / length, direction.Y / length);

        // Draw trajectory with up to 2 bounces
        var currentPos = start;
        var currentDir = normalizedDir;
        int maxBounces = 2;
        float maxDistance = 2000; // Maximum line length

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            // Calculate where line hits wall or top
            float distanceToLeftWall = (bubbleRadius - currentPos.X) / currentDir.X;
            float distanceToRightWall = (canvasWidth - bubbleRadius - currentPos.X) / currentDir.X;
            float distanceToTop = (150 - currentPos.Y) / currentDir.Y; // Top grid boundary

            // Find nearest collision (only consider positive distances)
            float minDistance = maxDistance;
            bool hitLeftWall = false;
            bool hitRightWall = false;
            bool hitTop = false;

            if (distanceToLeftWall > 0 && distanceToLeftWall < minDistance)
            {
                minDistance = distanceToLeftWall;
                hitLeftWall = true;
                hitRightWall = false;
                hitTop = false;
            }

            if (distanceToRightWall > 0 && distanceToRightWall < minDistance)
            {
                minDistance = distanceToRightWall;
                hitLeftWall = false;
                hitRightWall = true;
                hitTop = false;
            }

            if (distanceToTop > 0 && distanceToTop < minDistance)
            {
                minDistance = distanceToTop;
                hitLeftWall = false;
                hitRightWall = false;
                hitTop = true;
            }

            // Calculate end point of this segment
            var endPoint = new SKPoint(
                currentPos.X + currentDir.X * minDistance,
                currentPos.Y + currentDir.Y * minDistance
            );

            // Draw this segment
            canvas.DrawLine(currentPos, endPoint, paint);

            // Stop if hit top or reached max distance
            if (hitTop || minDistance >= maxDistance)
                break;

            // Reflect direction for next segment
            if (hitLeftWall || hitRightWall)
            {
                // Reflect X direction (bounce off vertical wall)
                currentDir = new SKPoint(-currentDir.X, currentDir.Y);

                // Clamp position to wall
                if (hitLeftWall)
                    endPoint.X = bubbleRadius;
                else
                    endPoint.X = canvasWidth - bubbleRadius;
            }

            currentPos = endPoint;
        }
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

        // Score
        canvas.DrawText($"Score: {state.Score}", 20, 40, _textPaint);

        // Level
        canvas.DrawText($"Level: {state.CurrentLevel}", 20, 70, _textPaint);

        // Combo
        if (state.Combo > 1)
        {
            canvas.DrawText($"Combo x{state.Combo}!", info.Width / 2 - 60, 40, _comboPaint);
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
            _gameEngine.GameState.IsPaused || _gameEngine.IsShootingInProgress ||
            _gameEngine.GameState.IsLevelComplete)
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
        // Prevent multiple timers from being created
        if (_isRunning)
            return;

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

using BubbleShot.Models;
using SkiaSharp;

namespace BubbleShot.Services;

public class GameEngine
{
    private const int MaxRows = 12;
    private const int MaxCols = 8;
    private const float BubbleSpacing = 3f;
    private const float ShootSpeed = 1000f;

    private List<Bubble> _bubbles;
    private Bubble? _currentBubble;
    private Bubble? _nextBubble;
    private SKPoint _shooterPosition;
    private float _canvasWidth;
    private float _canvasHeight;
    private Random _random;
    private float _bubbleRadius;

    public GameState GameState { get; private set; }
    public List<Bubble> Bubbles => _bubbles;
    public Bubble? CurrentBubble => _currentBubble;
    public Bubble? NextBubble => _nextBubble;
    public SKPoint ShooterPosition => _shooterPosition;
    public bool IsShootingInProgress { get; private set; }
    public SKPoint AimDirection { get; set; }

    private SKPoint _shootingBubblePosition;
    private SKPoint _shootingBubbleVelocity;

    public GameEngine()
    {
        _bubbles = new List<Bubble>();
        _random = new Random();
        GameState = new GameState();
        AimDirection = new SKPoint(0, -1);
    }

    public void InitializeGame(float canvasWidth, float canvasHeight, int level)
    {
        _canvasWidth = canvasWidth;
        _canvasHeight = canvasHeight;

        // Calculate bubble radius based on canvas width for better fit
        float maxBubbleWidth = (_canvasWidth - 40) / MaxCols; // 40px total margin
        _bubbleRadius = Math.Min(maxBubbleWidth / 2.3f, 35f); // Max 35px radius

        _shooterPosition = new SKPoint(canvasWidth / 2, canvasHeight - 120);

        GameState = new GameState { CurrentLevel = level };
        _bubbles.Clear();
        IsShootingInProgress = false;

        CreateBubbleGrid(level);
        CreateNewBubble();
        CreateNextBubble();
    }

    private void CreateBubbleGrid(int level)
    {
        int numRows = Math.Min(5 + level, MaxRows - 2);
        int numColors = Math.Min(4 + (level - 1) / 2, 6);

        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
        float startY = 80;

        for (int row = 0; row < numRows; row++)
        {
            int colsInRow = MaxCols;
            float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

            for (int col = 0; col < colsInRow; col++)
            {
                float x = startX + col * bubbleDiameter + offsetX;
                float y = startY + row * bubbleDiameter * 0.866f; // hexagonal spacing

                var color = GetRandomColor(numColors);
                var bubble = new Bubble(row, col, color, new SKPoint(x, y), _bubbleRadius);
                _bubbles.Add(bubble);
            }
        }

        GameState.BubblesRemaining = _bubbles.Count;
    }

    private BubbleColor GetRandomColor(int numColors)
    {
        var colors = new[] { BubbleColor.Red, BubbleColor.Blue, BubbleColor.Green,
                            BubbleColor.Yellow, BubbleColor.Purple, BubbleColor.Orange };
        return colors[_random.Next(numColors)];
    }

    private void CreateNewBubble()
    {
        if (_nextBubble != null)
        {
            _currentBubble = new Bubble(
                -1, -1,
                _nextBubble.Color,
                _shooterPosition,
                _bubbleRadius
            );
        }
        else
        {
            var color = GetRandomColor(Math.Min(4 + (GameState.CurrentLevel - 1) / 2, 6));
            _currentBubble = new Bubble(-1, -1, color, _shooterPosition, _bubbleRadius);
        }
    }

    private void CreateNextBubble()
    {
        var color = GetRandomColor(Math.Min(4 + (GameState.CurrentLevel - 1) / 2, 6));
        // Position next bubble in bottom right corner, away from the grid
        var nextPos = new SKPoint(_canvasWidth - 60, _canvasHeight - 120);
        _nextBubble = new Bubble(-1, -1, color, nextPos, _bubbleRadius * 0.7f);
    }

    public void StartShooting(SKPoint direction)
    {
        if (IsShootingInProgress || _currentBubble == null || GameState.IsPaused || GameState.IsGameOver)
            return;

        IsShootingInProgress = true;
        _shootingBubblePosition = _currentBubble.Position;

        // Normalize direction
        float length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        _shootingBubbleVelocity = new SKPoint(
            direction.X / length * ShootSpeed,
            direction.Y / length * ShootSpeed
        );
    }

    public void Update(float deltaTime)
    {
        if (GameState.IsPaused || GameState.IsGameOver)
            return;

        UpdatePoppingAnimation(deltaTime);

        if (IsShootingInProgress)
        {
            UpdateShootingBubble(deltaTime);
        }
    }

    private void UpdatePoppingAnimation(float deltaTime)
    {
        var poppingBubbles = _bubbles.Where(b => b.IsPopping).ToList();
        foreach (var bubble in poppingBubbles)
        {
            bubble.PopAnimationProgress += deltaTime * 3;
            if (bubble.PopAnimationProgress >= 1)
            {
                _bubbles.Remove(bubble);
            }
        }
    }

    private void UpdateShootingBubble(float deltaTime)
    {
        if (_currentBubble == null)
            return;

        // Update position
        _shootingBubblePosition.X += _shootingBubbleVelocity.X * deltaTime;
        _shootingBubblePosition.Y += _shootingBubbleVelocity.Y * deltaTime;

        // Wall collision
        if (_shootingBubblePosition.X - _bubbleRadius < 0)
        {
            _shootingBubblePosition.X = _bubbleRadius;
            _shootingBubbleVelocity.X *= -1;
        }
        else if (_shootingBubblePosition.X + _bubbleRadius > _canvasWidth)
        {
            _shootingBubblePosition.X = _canvasWidth - _bubbleRadius;
            _shootingBubbleVelocity.X *= -1;
        }

        // Check collision with existing bubbles
        foreach (var bubble in _bubbles.Where(b => !b.IsPopping))
        {
            if (bubble.CollidesWith(_shootingBubblePosition, _bubbleRadius))
            {
                AttachBubble(_shootingBubblePosition, _currentBubble.Color);
                return;
            }
        }

        // Check if reached top
        if (_shootingBubblePosition.Y - _bubbleRadius <= 80)
        {
            AttachBubble(_shootingBubblePosition, _currentBubble.Color);
        }

        _currentBubble.Position = _shootingBubblePosition;
    }

    private void AttachBubble(SKPoint position, BubbleColor color)
    {
        // Find the closest grid position
        var (row, col) = FindClosestGridPosition(position);

        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
        float startY = 80;
        float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

        float gridX = startX + col * bubbleDiameter + offsetX;
        float gridY = startY + row * bubbleDiameter * 0.866f;

        var newBubble = new Bubble(row, col, color, new SKPoint(gridX, gridY), _bubbleRadius);
        _bubbles.Add(newBubble);

        // Check for matches
        var matchingBubbles = FindMatchingBubbles(newBubble);
        if (matchingBubbles.Count >= 3)
        {
            PopBubbles(matchingBubbles);
            RemoveOrphanedBubbles();
            GameState.AddScore(matchingBubbles.Count);
        }
        else
        {
            GameState.ResetCombo();
        }

        // Check win/lose conditions
        CheckGameConditions();

        // Prepare next bubble
        IsShootingInProgress = false;
        CreateNewBubble();
        CreateNextBubble();
    }

    private (int row, int col) FindClosestGridPosition(SKPoint position)
    {
        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
        float startY = 80;

        int row = (int)Math.Round((position.Y - startY) / (bubbleDiameter * 0.866f));
        row = Math.Max(0, Math.Min(row, MaxRows - 1));

        float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;
        int col = (int)Math.Round((position.X - startX - offsetX) / bubbleDiameter);
        col = Math.Max(0, Math.Min(col, MaxCols - 1));

        // Check if position is occupied
        while (_bubbles.Any(b => b.Row == row && b.Col == col && !b.IsPopping))
        {
            row--;
            if (row < 0) row = 0;
        }

        return (row, col);
    }

    private List<Bubble> FindMatchingBubbles(Bubble startBubble)
    {
        var matching = new List<Bubble>();
        var toCheck = new Queue<Bubble>();
        var checked_ = new HashSet<Bubble>();

        toCheck.Enqueue(startBubble);
        checked_.Add(startBubble);

        while (toCheck.Count > 0)
        {
            var current = toCheck.Dequeue();
            matching.Add(current);

            var neighbors = GetNeighbors(current);
            foreach (var neighbor in neighbors)
            {
                if (!checked_.Contains(neighbor) && neighbor.Color == startBubble.Color && !neighbor.IsPopping)
                {
                    checked_.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        return matching;
    }

    private List<Bubble> GetNeighbors(Bubble bubble)
    {
        var neighbors = new List<Bubble>();
        int row = bubble.Row;
        int col = bubble.Col;
        bool isOddRow = row % 2 == 1;

        // Hexagonal grid neighbors
        var offsets = isOddRow
            ? new[] { (-1, 0), (-1, 1), (0, -1), (0, 1), (1, 0), (1, 1) }
            : new[] { (-1, -1), (-1, 0), (0, -1), (0, 1), (1, -1), (1, 0) };

        foreach (var (rowOffset, colOffset) in offsets)
        {
            int newRow = row + rowOffset;
            int newCol = col + colOffset;

            var neighbor = _bubbles.FirstOrDefault(b =>
                b.Row == newRow && b.Col == newCol && !b.IsPopping);

            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private void PopBubbles(List<Bubble> bubbles)
    {
        foreach (var bubble in bubbles)
        {
            bubble.IsPopping = true;
            bubble.PopAnimationProgress = 0;
        }
    }

    private void RemoveOrphanedBubbles()
    {
        var connected = new HashSet<Bubble>();
        var toCheck = new Queue<Bubble>();

        // Start from top row
        var topBubbles = _bubbles.Where(b => b.Row == 0 && !b.IsPopping).ToList();
        foreach (var bubble in topBubbles)
        {
            toCheck.Enqueue(bubble);
            connected.Add(bubble);
        }

        // Find all connected bubbles
        while (toCheck.Count > 0)
        {
            var current = toCheck.Dequeue();
            var neighbors = GetNeighbors(current);

            foreach (var neighbor in neighbors)
            {
                if (!connected.Contains(neighbor) && !neighbor.IsPopping)
                {
                    connected.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        // Pop orphaned bubbles
        var orphaned = _bubbles.Where(b => !connected.Contains(b) && !b.IsPopping).ToList();
        if (orphaned.Count > 0)
        {
            PopBubbles(orphaned);
            GameState.AddScore(orphaned.Count * 2); // Bonus points for dropped bubbles
        }
    }

    private void CheckGameConditions()
    {
        var activeBubbles = _bubbles.Where(b => !b.IsPopping).ToList();

        if (activeBubbles.Count == 0)
        {
            GameState.IsLevelComplete = true;
        }

        // Check if bubbles reached bottom
        if (activeBubbles.Any(b => b.Position.Y + _bubbleRadius > _canvasHeight - 200))
        {
            GameState.IsGameOver = true;
        }
    }

    public SKPoint GetShootingBubblePosition()
    {
        return IsShootingInProgress ? _shootingBubblePosition : _currentBubble?.Position ?? _shooterPosition;
    }
}

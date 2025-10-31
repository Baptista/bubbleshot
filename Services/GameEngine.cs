using BubbleShot.Models;
using SkiaSharp;

namespace BubbleShot.Services;

public class GameEngine
{
    private const int MaxCols = 7;  // Reduced from 8 for bigger bubbles
    private const float BubbleSpacing = 2f;  // Reduced spacing
    private const float ShootSpeed = 1000f;
    private const float VisibleRows = 5.5f;  // Number of rows visible on screen at once

    private List<Bubble> _bubbles;
    private Bubble? _currentBubble;
    private Bubble? _nextBubble;
    private SKPoint _shooterPosition;
    private float _canvasWidth;
    private float _canvasHeight;
    private Random _random;
    private float _bubbleRadius;
    private float _gridOffsetY;  // Tracks how much the grid has scrolled down
    private float _gridStartY;   // Top of visible area
    private float _gridRow0Y;    // Current Y position where row 0 is located
    private int _totalRows;      // Total number of rows in current level

    public GameState GameState { get; private set; }
    public List<Bubble> Bubbles => _bubbles;
    public Bubble? CurrentBubble => _currentBubble;
    public Bubble? NextBubble => _nextBubble;
    public SKPoint ShooterPosition => _shooterPosition;
    public bool IsShootingInProgress { get; private set; }
    public SKPoint AimDirection { get; set; }

    // Expose canvas dimensions for restart
    public float CanvasWidth => _canvasWidth;
    public float CanvasHeight => _canvasHeight;

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

        // Calculate bubble radius - use MUCH more screen space!
        float maxBubbleWidth = (_canvasWidth - 10) / MaxCols; // Minimal margin
        _bubbleRadius = (maxBubbleWidth - BubbleSpacing) / 2; // Maximum possible bubble size

        _shooterPosition = new SKPoint(canvasWidth / 2, canvasHeight - 150);

        GameState = new GameState { CurrentLevel = level };
        _bubbles.Clear();
        IsShootingInProgress = false;

        CreateBubbleGrid(level);
        CreateNewBubble();
        CreateNextBubble();
    }

    private void CreateBubbleGrid(int level)
    {
        // Start with visible rows only - no hidden rows above
        int initialRows = Math.Min(4 + (level - 1) / 2, 6);  // Level 1: 4 rows, increases to max 6 visible rows
        _totalRows = initialRows;
        int numColors = Math.Min(4 + (level - 1) / 2, 6);

        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;

        // Start at top of visible area - ALL bubbles visible from start
        _gridStartY = 150;
        _gridRow0Y = _gridStartY;  // Row 0 starts at top of visible area
        _gridOffsetY = 0;

        for (int row = 0; row < _totalRows; row++)
        {
            // Odd rows have one fewer column to stay within bounds when offset
            int colsInRow = (row % 2 == 1) ? MaxCols - 1 : MaxCols;
            float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

            for (int col = 0; col < colsInRow; col++)
            {
                float x = startX + col * bubbleDiameter + offsetX;
                float y = _gridRow0Y + row * bubbleDiameter * 0.866f;

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

    private BubbleColor GetRandomColorFromGrid()
    {
        // Get all unique colors currently in the grid (not popping)
        var gridColors = new HashSet<BubbleColor>();
        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (!_bubbles[i].IsPopping && _bubbles[i].Color != BubbleColor.Empty)
            {
                gridColors.Add(_bubbles[i].Color);
            }
        }

        // If no bubbles in grid (shouldn't happen), use level-based colors
        if (gridColors.Count == 0)
        {
            return GetRandomColor(Math.Min(4 + (GameState.CurrentLevel - 1) / 2, 6));
        }

        // Select random color from grid colors
        var colorList = gridColors.ToArray();
        return colorList[_random.Next(colorList.Length)];
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
            // First bubble - use color from grid
            var color = GetRandomColorFromGrid();
            _currentBubble = new Bubble(-1, -1, color, _shooterPosition, _bubbleRadius);
        }
    }

    private void CreateNextBubble()
    {
        // Always select from colors that exist in the grid
        var color = GetRandomColorFromGrid();
        // Position next bubble in bottom right corner, away from the grid
        var nextPos = new SKPoint(_canvasWidth - 70, _canvasHeight - 150);
        _nextBubble = new Bubble(-1, -1, color, nextPos, _bubbleRadius * 0.8f);  // Slightly bigger preview
    }

    private void ValidateNextBubbleColor()
    {
        // Check if next bubble's color still exists in the grid
        if (_nextBubble == null)
            return;

        bool colorExistsInGrid = false;
        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (!_bubbles[i].IsPopping && _bubbles[i].Row >= 0 &&
                _bubbles[i].Color == _nextBubble.Color)
            {
                colorExistsInGrid = true;
                break;
            }
        }

        // If next bubble's color doesn't exist in grid anymore, regenerate it
        if (!colorExistsInGrid)
        {
            CreateNextBubble();
        }
    }

    public void StartShooting(SKPoint direction)
    {
        if (IsShootingInProgress || _currentBubble == null || GameState.IsPaused ||
            GameState.IsGameOver || GameState.IsLevelComplete)
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
        // Always update popping animation, even when game is over or level complete
        UpdatePoppingAnimation(deltaTime);

        // Stop game updates if paused, game over, or level complete
        if (GameState.IsPaused || GameState.IsGameOver || GameState.IsLevelComplete)
            return;

        if (IsShootingInProgress)
        {
            UpdateShootingBubble(deltaTime);
        }
    }

    private void UpdatePoppingAnimation(float deltaTime)
    {
        // Avoid LINQ allocation - use for loop with manual removal
        bool anyRemoved = false;
        for (int i = _bubbles.Count - 1; i >= 0; i--)
        {
            var bubble = _bubbles[i];
            if (bubble.IsPopping)
            {
                bubble.PopAnimationProgress += deltaTime * 3;
                if (bubble.PopAnimationProgress >= 1)
                {
                    _bubbles.RemoveAt(i);
                    anyRemoved = true;
                }
            }
        }

        // Check if we should scroll the grid down after removing bubbles
        if (anyRemoved)
        {
            ScrollGridIfNeeded();
        }

        // Re-check game conditions after removing bubbles to ensure level complete is detected
        if (anyRemoved && !GameState.IsLevelComplete && !GameState.IsGameOver)
        {
            CheckGameConditions();
        }
    }

    private void ScrollGridIfNeeded()
    {
        // Find the lowest row that still has bubbles
        int lowestRowWithBubbles = -1;
        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (!_bubbles[i].IsPopping && _bubbles[i].Row >= 0)
            {
                if (lowestRowWithBubbles == -1 || _bubbles[i].Row < lowestRowWithBubbles)
                {
                    lowestRowWithBubbles = _bubbles[i].Row;
                }
            }
        }

        // If lowest row is above row 0, we have cleared some rows
        if (lowestRowWithBubbles > 0)
        {
            int rowsCleared = lowestRowWithBubbles;
            float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
            float scrollAmount = rowsCleared * bubbleDiameter * 0.866f;

            // Add new rows from above BEFORE scrolling
            float gridWidth = MaxCols * bubbleDiameter;
            float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
            int numColors = Math.Min(4 + (GameState.CurrentLevel - 1) / 2, 6);

            // Add new rows at negative row numbers (above row 0)
            for (int row = -rowsCleared; row < 0; row++)
            {
                int colsInRow = (row % 2 == 1) ? MaxCols - 1 : MaxCols;
                float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

                for (int col = 0; col < colsInRow; col++)
                {
                    float x = startX + col * bubbleDiameter + offsetX;
                    float y = _gridRow0Y + row * bubbleDiameter * 0.866f;

                    var color = GetRandomColorFromGrid();
                    var bubble = new Bubble(row, col, color, new SKPoint(x, y), _bubbleRadius);
                    _bubbles.Add(bubble);
                }
            }

            // NOW scroll everything down
            for (int i = 0; i < _bubbles.Count; i++)
            {
                _bubbles[i].Position = new SKPoint(
                    _bubbles[i].Position.X,
                    _bubbles[i].Position.Y + scrollAmount
                );

                // Update row number (shift all rows down)
                _bubbles[i].Row -= lowestRowWithBubbles;
            }

            _gridOffsetY += scrollAmount;
            _gridRow0Y += scrollAmount;
            _totalRows += rowsCleared;  // Increase total row count
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
        int bubbleCount = _bubbles.Count;
        for (int i = 0; i < bubbleCount; i++)
        {
            var bubble = _bubbles[i];
            if (bubble.IsPopping || bubble.Row < 0)  // Skip popping bubbles and shooter bubbles
                continue;

            if (bubble.CollidesWith(_shootingBubblePosition, _bubbleRadius))
            {
                AttachBubble(_shootingBubblePosition, _currentBubble.Color);
                return;
            }
        }

        // Check if reached the top boundary (above all bubbles)
        if (_shootingBubblePosition.Y - _bubbleRadius <= _gridStartY)
        {
            AttachBubble(_shootingBubblePosition, _currentBubble.Color);
            return;
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
        float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

        float gridX = startX + col * bubbleDiameter + offsetX;
        float gridY = _gridRow0Y + row * bubbleDiameter * 0.866f;

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

        // Check win/lose conditions FIRST
        CheckGameConditions();

        // Prepare next bubble (only if game is still active)
        IsShootingInProgress = false;
        if (!GameState.IsLevelComplete && !GameState.IsGameOver)
        {
            // Validate next bubble color after clearing bubbles (only if game continues)
            ValidateNextBubbleColor();
            CreateNewBubble();
            CreateNextBubble();
        }
    }

    private bool IsPositionOccupied(int row, int col)
    {
        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (_bubbles[i].Row == row && _bubbles[i].Col == col && !_bubbles[i].IsPopping)
                return true;
        }
        return false;
    }

    private (int row, int col) FindClosestGridPosition(SKPoint position)
    {
        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;

        int row = (int)Math.Round((position.Y - _gridRow0Y) / (bubbleDiameter * 0.866f));

        // Clamp row to valid range
        row = Math.Max(0, Math.Min(row, _totalRows - 1));

        bool isOddRow = row % 2 == 1;
        int maxColForRow = isOddRow ? MaxCols - 2 : MaxCols - 1;
        float offsetX = isOddRow ? bubbleDiameter / 2 : 0;
        int col = (int)Math.Round((position.X - startX - offsetX) / bubbleDiameter);
        col = Math.Max(0, Math.Min(col, maxColForRow));

        // Check if position is occupied - find nearest empty spot (optimized)
        if (IsPositionOccupied(row, col))
        {
            // Try to find empty adjacent positions
            var candidates = new List<(int row, int col, float distance)>();

            // Check all nearby positions in a 3x3 grid
            for (int r = Math.Max(0, row - 1); r <= Math.Min(_totalRows - 1, row + 1); r++)
            {
                bool rIsOdd = r % 2 == 1;
                int maxColForR = rIsOdd ? MaxCols - 2 : MaxCols - 1;
                int colStart = rIsOdd ? Math.Max(0, col - 1) : col - 1;
                int colEnd = Math.Min(maxColForR, rIsOdd ? col + 1 : col + 1);

                for (int c = Math.Max(0, colStart); c <= colEnd; c++)
                {
                    // Skip if occupied (optimized)
                    if (IsPositionOccupied(r, c))
                        continue;

                    // Calculate distance from original position
                    float candidateOffsetX = (r % 2 == 1) ? bubbleDiameter / 2 : 0;
                    float candidateX = startX + c * bubbleDiameter + candidateOffsetX;
                    float candidateY = _gridRow0Y + r * bubbleDiameter * 0.866f;
                    float dist = (position.X - candidateX) * (position.X - candidateX) +
                                 (position.Y - candidateY) * (position.Y - candidateY);

                    candidates.Add((r, c, dist));
                }
            }

            // Use the closest empty position
            if (candidates.Count > 0)
            {
                var closest = candidates.OrderBy(c => c.distance).First();
                row = closest.row;
                col = closest.col;
            }
            else
            {
                // Fallback: move up until we find empty spot (optimized)
                while (IsPositionOccupied(row, col) && row > 0)
                {
                    row--;
                    offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;
                    col = (int)Math.Round((position.X - startX - offsetX) / bubbleDiameter);
                    col = Math.Max(0, Math.Min(col, MaxCols - 1));
                }
            }
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

        int safetyCounter = 0;
        int maxIterations = 1000; // Safety limit to prevent infinite loops

        while (toCheck.Count > 0 && safetyCounter < maxIterations)
        {
            safetyCounter++;
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

            // Optimized - no LINQ, manual search
            for (int i = 0; i < _bubbles.Count; i++)
            {
                var candidate = _bubbles[i];
                if (candidate.Row == newRow && candidate.Col == newCol && !candidate.IsPopping)
                {
                    neighbors.Add(candidate);
                    break; // Found the neighbor, move to next offset
                }
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

        // Start from top row (optimized - no LINQ)
        for (int i = 0; i < _bubbles.Count; i++)
        {
            var bubble = _bubbles[i];
            if (bubble.Row == 0 && !bubble.IsPopping)
            {
                toCheck.Enqueue(bubble);
                connected.Add(bubble);
            }
        }

        int safetyCounter = 0;
        int maxIterations = 1000; // Safety limit to prevent infinite loops

        // Find all connected bubbles
        while (toCheck.Count > 0 && safetyCounter < maxIterations)
        {
            safetyCounter++;
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

        // Pop orphaned bubbles (optimized - no LINQ allocation)
        int orphanedCount = 0;
        for (int i = 0; i < _bubbles.Count; i++)
        {
            var bubble = _bubbles[i];
            if (!connected.Contains(bubble) && !bubble.IsPopping)
            {
                bubble.IsPopping = true;
                bubble.PopAnimationProgress = 0;
                orphanedCount++;
            }
        }

        if (orphanedCount > 0)
        {
            GameState.AddScore(orphanedCount * 2); // Bonus points for dropped bubbles
        }
    }

    private void CheckGameConditions()
    {
        // Optimized - count active bubbles and check bottom in one pass
        int activeBubbleCount = 0;
        bool reachedBottom = false;
        float bottomThreshold = _canvasHeight - 300;

        for (int i = 0; i < _bubbles.Count; i++)
        {
            var bubble = _bubbles[i];
            // Only count grid bubbles (row >= 0), not shooter bubbles (row = -1)
            if (!bubble.IsPopping && bubble.Row >= 0)
            {
                activeBubbleCount++;

                if (bubble.Position.Y + _bubbleRadius > bottomThreshold)
                {
                    reachedBottom = true;
                }
            }
        }

        if (activeBubbleCount == 0)
        {
            GameState.IsLevelComplete = true;
        }

        if (reachedBottom)
        {
            GameState.IsGameOver = true;
        }
    }

    public SKPoint GetShootingBubblePosition()
    {
        return IsShootingInProgress ? _shootingBubblePosition : _currentBubble?.Position ?? _shooterPosition;
    }
}

using BubbleShot.Models;
using SkiaSharp;

namespace BubbleShot.Services;

public class GameEngine
{
    private const int MaxRows = 12;
    private const int MaxCols = 7;  // Reduced from 8 for bigger bubbles
    private const float BubbleSpacing = 2f;  // Reduced spacing
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

    // Expose canvas dimensions for restart
    public float CanvasWidth => _canvasWidth;
    public float CanvasHeight => _canvasHeight;

    // Expose scroll offset for rendering
    public float ScrollOffset => _scrollOffset;

    private SKPoint _shootingBubblePosition;
    private SKPoint _shootingBubbleVelocity;

    // Scrolling viewport system
    private int _totalRowsForLevel;     // Total rows that need to be cleared for the level
    private int _visibleRows;           // Number of rows visible on screen (always 5)
    private float _scrollOffset;        // Vertical scroll offset in pixels
    private float _rowHeight;           // Height of one row

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
        // Calculate total rows for level progression
        // But we always have extra rows for stacking bubbles
        int bubblesRows = Math.Min(5 + (level - 1) / 3, 10); // Rows with initial bubbles
        _visibleRows = 10; // Always show 10 rows on screen
        _totalRowsForLevel = 20; // Total rows available (including off-screen)

        int numColors = Math.Min(4 + (level - 1) / 2, 6);

        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
        float startY = 150;  // Start higher up for more play space

        // Calculate row height for hexagonal grid
        _rowHeight = bubbleDiameter * 0.866f;

        // Generate ONLY the initial bubble rows at the BOTTOM
        // With reversed positioning: Row 0 = bottom, higher numbers = top
        // Fill only rows 0 to bubblesRows-1 (e.g., rows 0-4)
        for (int row = 0; row < bubblesRows; row++)
        {
            // Odd rows have one fewer column to stay within bounds when offset
            int colsInRow = (row % 2 == 1) ? MaxCols - 1 : MaxCols;
            float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

            for (int col = 0; col < colsInRow; col++)
            {
                float x = startX + col * bubbleDiameter + offsetX;
                // REVERSE: Higher row numbers have LOWER Y values (towards top)
                float y = startY + (_totalRowsForLevel - 1 - row) * _rowHeight;

                var color = GetRandomColor(numColors);
                var bubble = new Bubble(row, col, color, new SKPoint(x, y), _bubbleRadius);
                _bubbles.Add(bubble);
            }
        }

        // Initialize scroll offset to show bottom 10 rows (0-9)
        // Rows 0-4: filled with bubbles
        // Rows 5-9: empty (for shooting)
        // Rows 10-19: off-screen above
        int hiddenRows = Math.Max(0, _totalRowsForLevel - _visibleRows);
        _scrollOffset = hiddenRows * _rowHeight;

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

        // Re-check game conditions after removing bubbles to ensure level complete is detected
        if (anyRemoved && !GameState.IsLevelComplete && !GameState.IsGameOver)
        {
            // Update scroll position to reveal hidden rows as bubbles are cleared
            UpdateScrollPosition();

            CheckGameConditions();
        }
    }

    private void UpdateScrollPosition()
    {
        // Find the row with LOWEST row number (0, 1, 2...) that still has bubbles
        // With reversed positioning, this is the bottommost visible row
        int lowestRowNumber = -1;

        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (!_bubbles[i].IsPopping && _bubbles[i].Row >= 0)
            {
                if (lowestRowNumber == -1 || _bubbles[i].Row < lowestRowNumber)
                {
                    lowestRowNumber = _bubbles[i].Row;
                }
            }
        }

        // If no bubbles remain, no need to scroll
        if (lowestRowNumber == -1)
            return;

        // Keep showing rows from lowestRowNumber up to lowestRowNumber + visibleRows - 1
        // As row 0 clears, show rows 1-5; as rows 0,1 clear, show rows 2-6, etc.
        int targetTopRow = Math.Max(0, lowestRowNumber + _visibleRows - 1);
        targetTopRow = Math.Min(_totalRowsForLevel - 1, targetTopRow);

        int targetBottomRow = Math.Max(0, targetTopRow - _visibleRows + 1);

        // Calculate scroll offset needed to show targetBottomRow at top of viewport
        // Row worldY = 150 + (totalRows - 1 - row) * rowHeight
        // We want: screenY = 150 (top of viewport)
        // screenY = worldY - scrollOffset
        // 150 = 150 + (totalRows - 1 - targetTopRow) * rowHeight - scrollOffset
        // scrollOffset = (totalRows - 1 - targetTopRow) * rowHeight
        _scrollOffset = (_totalRowsForLevel - 1 - targetTopRow) * _rowHeight;

        // Clamp to valid range
        float maxScrollOffset = Math.Max(0, (_totalRowsForLevel - _visibleRows) * _rowHeight);
        _scrollOffset = Math.Min(_scrollOffset, maxScrollOffset);
        _scrollOffset = Math.Max(0, _scrollOffset);
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

        // Check collision with existing bubbles - optimize with early exit and no LINQ
        // Account for scroll offset: bubble positions are in world space, shooting bubble is in screen space
        // Only check bubbles that are visible on screen
        int bubbleCount = _bubbles.Count;
        float viewportTop = 150; // Top of visible area
        float viewportBottom = _canvasHeight - 150; // Bottom of visible area (where shooter is)

        for (int i = 0; i < bubbleCount; i++)
        {
            var bubble = _bubbles[i];
            if (bubble.IsPopping)
                continue;

            // Convert bubble world position to screen position for collision check
            SKPoint bubbleScreenPos = new SKPoint(bubble.Position.X, bubble.Position.Y - _scrollOffset);

            // Skip bubbles that are off-screen (outside visible viewport)
            if (bubbleScreenPos.Y < viewportTop - _bubbleRadius || bubbleScreenPos.Y > viewportBottom + _bubbleRadius)
                continue;

            float dx = _shootingBubblePosition.X - bubbleScreenPos.X;
            float dy = _shootingBubblePosition.Y - bubbleScreenPos.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < _bubbleRadius * 2)
            {
                AttachBubble(_shootingBubblePosition, _currentBubble.Color);
                return;
            }
        }

        // Check if reached top of visible area
        if (_shootingBubblePosition.Y - _bubbleRadius <= 150)
        {
            AttachBubble(_shootingBubblePosition, _currentBubble.Color);
            return;
        }

        // Safety check: if bubble somehow escaped bounds, attach it to nearest position
        if (_shootingBubblePosition.Y < 0 || _shootingBubblePosition.Y > _canvasHeight ||
            _shootingBubblePosition.X < 0 || _shootingBubblePosition.X > _canvasWidth)
        {
            AttachBubble(_shootingBubblePosition, _currentBubble.Color);
            return;
        }

        _currentBubble.Position = _shootingBubblePosition;
    }

    private void AttachBubble(SKPoint screenPosition, BubbleColor color)
    {
        // Convert screen position to world position
        SKPoint worldPosition = new SKPoint(screenPosition.X, screenPosition.Y + _scrollOffset);

        // Find the closest grid position
        var (row, col) = FindClosestGridPosition(worldPosition);

        // Safety check: if position is still occupied after FindClosestGridPosition,
        // don't add the bubble (this shouldn't happen but prevents duplicates)
        if (IsPositionOccupied(row, col))
        {
            // Position is occupied and no empty spot found - just end the shot
            IsShootingInProgress = false;
            if (!GameState.IsLevelComplete && !GameState.IsGameOver)
            {
                CreateNewBubble();
                CreateNextBubble();
            }
            return;
        }

        float bubbleDiameter = _bubbleRadius * 2 + BubbleSpacing;
        float gridWidth = MaxCols * bubbleDiameter;
        float startX = (_canvasWidth - gridWidth) / 2 + _bubbleRadius;
        float startY = 150;
        float offsetX = (row % 2 == 1) ? bubbleDiameter / 2 : 0;

        float gridX = startX + col * bubbleDiameter + offsetX;
        // Use reversed positioning formula
        float gridY = startY + (_totalRowsForLevel - 1 - row) * _rowHeight;

        var newBubble = new Bubble(row, col, color, new SKPoint(gridX, gridY), _bubbleRadius);
        _bubbles.Add(newBubble);

        // Update game state
        GameState.BubblesRemaining = _bubbles.Count;

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
        float startY = 150;

        // With reversed positioning: Row 9 at Y=150, Row 0 at Y=150+9*rowHeight
        int rowIndex = (int)Math.Round((position.Y - startY) / _rowHeight);
        int row = _totalRowsForLevel - 1 - rowIndex;

        // Clamp row to valid range
        row = Math.Max(0, Math.Min(row, _totalRowsForLevel - 1));

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
            for (int r = Math.Max(0, row - 1); r <= Math.Min(_totalRowsForLevel - 1, row + 1); r++)
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
                    // Use reversed positioning formula
                    float candidateY = startY + (_totalRowsForLevel - 1 - r) * _rowHeight;
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
                // Fallback: search in wider area for empty spot
                // With reversed positioning: higher row numbers = towards top, lower = towards bottom
                bool found = false;

                // Try progressively wider search radius
                for (int radius = 2; radius <= 5 && !found; radius++)
                {
                    for (int r = Math.Max(0, row - radius); r <= Math.Min(_totalRowsForLevel - 1, row + radius); r++)
                    {
                        bool rIsOdd = r % 2 == 1;
                        int maxColForR = rIsOdd ? MaxCols - 2 : MaxCols - 1;

                        for (int c = 0; c <= maxColForR; c++)
                        {
                            if (!IsPositionOccupied(r, c))
                            {
                                row = r;
                                col = c;
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                // If still no empty spot found, the grid might be full
                // Return the occupied position - caller will handle it
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

        // Start from top row (with reversed positioning, top row has HIGHEST row number)
        int topRow = _totalRowsForLevel - 1;
        for (int i = 0; i < _bubbles.Count; i++)
        {
            var bubble = _bubbles[i];
            if (bubble.Row == topRow && !bubble.IsPopping)
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
        // Optimized - count active bubbles and check highest row reached
        int activeBubbleCount = 0;
        int highestRowReached = -1; // Highest row number with bubbles

        for (int i = 0; i < _bubbles.Count; i++)
        {
            var bubble = _bubbles[i];
            // Only count grid bubbles (row >= 0), not shooter bubbles (row = -1)
            if (!bubble.IsPopping && bubble.Row >= 0)
            {
                activeBubbleCount++;

                // Track highest row number (remember: higher numbers = towards top)
                if (highestRowReached == -1 || bubble.Row > highestRowReached)
                {
                    highestRowReached = bubble.Row;
                }
            }
        }

        // Win condition: All bubbles cleared
        if (activeBubbleCount == 0)
        {
            GameState.IsLevelComplete = true;
        }

        // Lose condition: Bubbles stacked too high
        // With 10 visible rows (0-9), lose if bubbles reach row 9 (top of visible area)
        // This leaves rows 5-8 as safe play area
        int dangerRow = 9; // Adjust this for difficulty
        if (highestRowReached >= dangerRow)
        {
            GameState.IsGameOver = true;
        }
    }

    public SKPoint GetShootingBubblePosition()
    {
        return IsShootingInProgress ? _shootingBubblePosition : _currentBubble?.Position ?? _shooterPosition;
    }
}
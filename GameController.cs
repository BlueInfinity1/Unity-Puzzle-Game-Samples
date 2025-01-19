using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/* This represents a game controller class for a tile-based puzzle game that has uses an m x n grid, and (m-2) x (n-2) puzzle pieces on the middle of the grid.
 * For example, we may have a 3x3 puzzle on 5x5 grid. The puzzle pieces get sorted randomly in the middle and the task of the player is to reorganize the pieces by dragging
 * to put the puzzle pieces back to the original order, or any other order where the 4 patterns on the tile (up, down, right, left) match the patterns of the adjacent tiles.
 * For example, if a tile A at (0,0) has pattern X on its "right" slot, we must have the same pattern X on the "left" slot of the tile B at (1,0). */

[RequireComponent(typeof(TileSwipeController))]
public class GameController : MonoBehaviour
{
    Vector2Int squareGridSize;    
    [SerializeField] GameObject squareGrid;       
    [SerializeField] GameObject puzzleTilePrefab;    
    [SerializeField] Transform puzzleTileParent;
    [SerializeField] Transform highlighter;

    [SerializeField] GameObject scorePopup, pauseMenu;

    [HideInInspector] public Vector2 screenSize = new(640, 960);
    
    public int tileSize;
    public GridSquare[,] tileGrid;
    [HideInInspector] public Vector2 boardTopLeftCorner;
    
    TileSwipeController tileSwipeController;

    public int moveCount = 0;
    float levelTimer, levelTimerStartTime;
    [SerializeField] TextMeshProUGUI timerText, moveText;
       
    void Start()
    {
        if (!GameData.gameInitialized) // NOTE: This should be done in the main menu controller, and this is just here for the sake of quickly starting directly from the play mode scene
            GameData.InitializeGame();

        AudioManager.Instance.StopAllMusic();
        AudioManager.Instance.Play(Sound.MainBackgroundTheme, true, false);

        squareGridSize = new Vector2Int((int)GameData.gridSize.x, (int)GameData.gridSize.y);
        tileSwipeController = GetComponent<TileSwipeController>();
        levelTimerStartTime = Time.time;
        boardTopLeftCorner = new((int)Mathf.Round((screenSize.x - (squareGridSize.x * tileSize)) * 0.5f), (int)Mathf.Round((screenSize.y - (squareGridSize.y * tileSize)) * 0.5f));

        GenerateEmptyTileGrid();
        GeneratePuzzleTiles(); // This will generate the puzzle pieces in order, i.e. this is one solved state of the puzzle (there can be others as well)
        ShufflePuzzleTiles();
                
        StartCoroutine(UpdatingTimer());        
    }

    void GenerateEmptyTileGrid()
    {
        // We have a single object that represents the whole grid, and this object has a tiled texture
        Vector2 boardCenterPointInScreenSpace = boardTopLeftCorner + tileSize * 0.5f * (Vector2)squareGridSize;
        Vector2 boardCenterPointInWorldSpace = Camera.main.ScreenToWorldPoint(boardCenterPointInScreenSpace);
        squareGrid.transform.position = boardCenterPointInWorldSpace;
        squareGrid.GetComponent<SpriteRenderer>().size = squareGridSize * new Vector2(0.5f, 0.5f);
        tileGrid = new GridSquare[squareGridSize.x, squareGridSize.y];

        for (int i = 0; i < squareGridSize.x; i++)
        {
            for (int j = 0; j < squareGridSize.y; j++)
                tileGrid[i, j] = new GridSquare(null);
        }
    }

    void GeneratePuzzleTiles()
    {
        Vector2 worldSpacePosition;

        // Assuming we have one empty row/column on the edges
        for (int i = 1; i < squareGridSize.x - 1; i++)
        {
            for (int j = 1; j < squareGridSize.y - 1; j++)
            {
                // NOTE: Unity uses two different coordinate systems, the world position and the screen position. Screen position y increases as we go downwards, but world position y increases as we go upwards.
                // This is why we need to reverse to y coordinate calculation here to put the tiles in the correct rows.
                worldSpacePosition = Camera.main.ScreenToWorldPoint(new Vector3(boardTopLeftCorner.x + (i + 0.5f) * tileSize, boardTopLeftCorner.y + (squareGridSize.y - 1 - j + 0.5f) * tileSize));
                GameObject puzzleTileObject = Instantiate(puzzleTilePrefab, worldSpacePosition, Quaternion.identity);

                Tile puzzleTile = puzzleTileObject.GetComponent<Tile>();
                puzzleTile.GenerateRandomTilePatterns(7);
                // NOTE: We don't really need to assign all of the patterns here, since most of them will get changed, but since this is only executed once and the grid is never very big, it doesn't really matter

                tileGrid[i, j].tile = puzzleTile;
                puzzleTile.transform.SetParent(puzzleTileParent);

                if (i > 1)
                    tileGrid[i, j].tile.leftPattern = tileGrid[i - 1, j].tile.rightPattern;
                if (j > 1)
                    tileGrid[i, j].tile.topPattern = tileGrid[i, j - 1].tile.bottomPattern;
                if (i == squareGridSize.x - 2)
                    tileGrid[i, j].tile.rightPattern = tileGrid[1, j].tile.leftPattern;
                if (j == squareGridSize.y - 2)
                    tileGrid[i, j].tile.bottomPattern = tileGrid[i, 1].tile.topPattern;

                tileGrid[i, j].tile.SetPatternImages();
            }
        }
    }

    void ShufflePuzzleTiles()
    {
        Tile temp;
        int randomColumn, randomRow;

        // NOTE: There's a small chance that the following algorithm can produce a puzzle that has already been solved, if half of the swaps cancel the other half out, e.g. we first swap (1,1) with (2,2),
        // and then swap (2,2) with (1,1) later on, which makes us end up with the original situation.

        // Go through all the tiles and swap their places in the grid with another tile
        for (int i = 1; i < squareGridSize.x - 1; i++)
        {
            for (int j = 1; j < squareGridSize.y - 1; j++)
            {
                randomColumn = Mathf.FloorToInt(Random.Range(1, squareGridSize.x - 1));
                randomRow = Mathf.FloorToInt(Random.Range(1, squareGridSize.y - 1));

                if (randomColumn == i && randomRow == j) //no swapping required
                    continue;

                temp = tileGrid[i, j].tile;
                tileGrid[i, j].tile = tileGrid[randomColumn, randomRow].tile;
                tileGrid[randomColumn, randomRow].tile = temp;

                Vector3 oldTilePos = GetTileWorldSpacePosition(new Vector2(i, j));
                Vector3 newPos = GetTileWorldSpacePosition(new Vector2(randomColumn, randomRow));

                tileGrid[i, j].tile.transform.position = oldTilePos;
                tileGrid[randomColumn, randomRow].tile.transform.position = newPos;            
            }
        }
    }

    bool IsPuzzleSolved()
    {
        Vector2Int startingTilePosition = Vector2Int.zero; // Initialize this to default value
        bool startingTileFound = false;
        // Since the puzzle does not have to be centered in the middle, get the tile grid position of the first tile we find from the upper-left, and start the check from this position
        for (int i = 0; i < squareGridSize.x; i++)
        {
            for (int j = 0; j < squareGridSize.x; j++)
            {
                if (tileGrid[i, j].tile != null)
                {
                    startingTilePosition = new Vector2Int(i, j);
                    startingTileFound = true;
                    break;
                }
            }

            if (startingTileFound)
                break;
        }

        Vector2Int endingTilePosition = startingTilePosition + squareGridSize - 3 * Vector2Int.one; // The position of the last tile we'll check
        // E.g. if first tile is at (2,2), then the ending position on a 4x4 grid size will be (3,3)

        // If the first tile we come across is at such a position that there's no room to have enough tiles to the right or downwards to fill the whole puzzle tile square, then the puzzle cannot be solved
        if (endingTilePosition.x >= squareGridSize.x || endingTilePosition.y >= squareGridSize.y)
            return false;

        for (int i = startingTilePosition.x; i < endingTilePosition.x + 1; i++)
        {
            for (int j = startingTilePosition.y; j < endingTilePosition.y + 1; j++)
            {
                if (tileGrid[i, j].tile == null) // All puzzle tiles must be next to each other, i.e. there can be no empty squares                        
                    return false;

                // Check matches between each of the tiles
                if ((i > startingTilePosition.x && tileGrid[i, j].tile.leftPattern != tileGrid[i - 1, j].tile.rightPattern) ||
                    (j > startingTilePosition.y && tileGrid[i, j].tile.topPattern != tileGrid[i, j - 1].tile.bottomPattern) ||
                    (i == endingTilePosition.x + 1 && tileGrid[i, j].tile.rightPattern != tileGrid[1, j].tile.leftPattern) ||
                    (j == endingTilePosition.y + 1 && tileGrid[i, j].tile.bottomPattern != tileGrid[i, 1].tile.topPattern))
                    return false;

            }
        }

        // Tf there are no mismatched patterns, we've solved the puzzle
        return true;
    }
    
    void FinalizeLevelClearing()
    {
        scorePopup.SetActive(true);
        
        int newScore = CalculateScore();
        scorePopup.GetComponent<ScoresMenuController>().SubmitNewHighScore(newScore); // Add a new top score if it fits within the top 10
    }

    int CalculateScore() // Calculate the score based on the grid size, used moves and used time
    {        
        // A quick sample formula. Score for moves is 0 if more than 100 moves are spent, and score for time is 0 if more than 100 seconds have been spent
        int score = squareGridSize.x * 50 + Mathf.Max(100 - moveCount, 0) * 2 + Mathf.Max(100 - (int)levelTimer, 0);
        return score;
    }
    
    void Update()
    {
        // This contains some useful debug hot keys
        if (GameData.debugModeOn)
        {
            if (Input.GetKeyDown(KeyCode.R)) // Restart scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            if (Input.GetKeyDown(KeyCode.S)) // Reshuffle
                ShufflePuzzleTiles();

            if (Input.GetKeyDown(KeyCode.T)) // Reset time and moves
            {
                levelTimerStartTime = Time.time;
                moveCount = 0;
                moveText.text = "Moves: 0";
            }

            if (Input.GetKeyDown(KeyCode.P))
                PrintTileGrid();

            if (Input.GetKeyDown(KeyCode.Escape))
                SceneManager.LoadScene("Main Menu");
        }
    }
    
    IEnumerator UpdatingTimer()
    {
        WaitForSeconds waitTime = new (0.1f); // Make this coroutine run every 0.1 seconds to update the time. This could be even more spaced out though, if the time is only updated once per second.
        while (true)
        {
            levelTimer = Time.time - levelTimerStartTime;
            timerText.text = "Time: " + GameUtilities.ConvertToTimeFormat(levelTimer);
            yield return waitTime;
        }
    }    

    /// <summary>
    /// Converts a tile's position in a grid (given as integer indices i, j) into its world space position.
    /// The input tile position represents the row and column indices of a tile in an m x n grid,
    /// not its coordinate position in world or screen space.
    /// </summary>
    /// <param name="tilePositionInGrid">The tile's position in the grid, where x is the column index and y is the row index.</param>
    /// <returns>The tile's world space position as a Vector3.</returns>
    public Vector3 GetTileWorldSpacePosition(Vector2 tilePositionInGrid)
    {
        Vector3 screenSpacePosition = new (boardTopLeftCorner.x + (tilePositionInGrid.x + 0.5f) * tileSize, boardTopLeftCorner.y + (squareGridSize.y - 1 - tilePositionInGrid.y + 0.5f) * tileSize);
        Vector2 worldSpacePosition = Camera.main.ScreenToWorldPoint(screenSpacePosition); // We use Vector2 cast here to make z coordinate 0
        return worldSpacePosition;
    }
    
    public void OnMovePlayed()
    {
        moveCount++;
        moveText.text = "Moves: " + moveCount;

        if (IsPuzzleSolved())
        {            
            tileSwipeController.enabled = false; // Disable any further swipe movements
            AudioManager.Instance.Play(Sound.LevelComplete);
            StopCoroutine(UpdatingTimer());
            Invoke(nameof(FinalizeLevelClearing), 1.0f);

            Debug.Log("<color=yellow>Puzzle has been solved!</color>");
        }
        else
            Debug.Log("Puzzle has NOT been solved yet.");
    }

    public void PauseGame()
    {
        AudioManager.Instance.Play(Sound.MenuButtonPress);
        pauseMenu.SetActive(true);
        tileSwipeController.enabled = false;
        Time.timeScale = 0; // NOTE: We do this to pause the timer, as Time.time won't update if this is 0. However, this will also affect all motion and physics simulations in the game, pausing them.
        // Since there are currently no physics objects or other objects relying on Time.time that would need to be running while the pause menu is active, we can do this safely.
    }

    public void UnpauseGame()
    {
        tileSwipeController.enabled = true;
        Time.timeScale = 1;        
    }

    public void PrintTileGrid()
    {
        // A function that may come useful in debugging. Prints out the tile grid contents to console using the following symbols: X - null square, O - occupied square
        string tileGridString = "";
        
        for (int i = 0; i < squareGridSize.y; i++) // NOTE: We've swapped the places of i and j here since we want to print one row at a time instead of a column
        {
            for (int j = 0; j < squareGridSize.x; j++)            
                tileGridString += tileGrid[j, i].tile == null ? "X " : "O ";
            
            tileGridString += "\n";
        }

        Debug.Log(tileGridString);
    }
}

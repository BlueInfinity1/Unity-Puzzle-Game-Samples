using UnityEngine;

/* This class controls the tile swipes used to move tiles in the puzzle game. */

[RequireComponent(typeof(GameController))]
public class TileSwipeController : MonoBehaviour
{
    Vector2 dragStartPoint;
    Vector2 dragStartTileGridPosition;
    Vector2 tileDragMovement;

    GameController gameController;
    [SerializeField] Transform markerArrow;
    SpriteRenderer arrowRenderer;
    [SerializeField] Transform highlighter;
        
    void Start()
    {        
        gameController = GetComponent<GameController>();
        dragStartTileGridPosition = -Vector2.one; // (-1, -1) signifies that the position has not been set or is not within our grid
        arrowRenderer = markerArrow.GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        /* NOTE: Even though we're using mouse button functions here, these functions also work on mobile devices, as long as Input.simulateMouseWithTouches = true.
        * For the case of this game, where only a single finger is required to play the game, this is all that's required. However, if more flexibility with touch controls is wanted (e.g. using two fingers),
        * Input.GetTouch(0) could be used, and we would then evaluate whether Input.GetTouch(0).phase equals TouchPhase.Began, TouchPhase.Moved, TouchPhase.Ended, which correspond fairly well to the mouse functions
        * GetMouseButtonDown, GetMouseButton and GetMouseButtonUp.
        *
        * The Input.GetMouseButton and Input.Touch functions are a part of the older Input system that Unity uses. This system is still perfectly valid, and many developers feel that 
        * this system is more intuitive than the new one. I've personally used both, and I feel that the new input system comes in handy if you want to tie multiple controls, e.g. a keyboard button, touch, mouse click
        * and a gamepad button, to the same action, such as "accept", "select", "cancel", etc. After setting up the controls for a specific action, you then observe the action events in your code instead of 
        * direct keypresses.
        *
        * However, in this case, this kind of control mapping or other nuances the new system offers are not required, so I felt that the old input system would suit the needs of this game better.
        */

        Vector2 currentMousePosition = new(Input.mousePosition.x, gameController.screenSize.y - Input.mousePosition.y);
        Vector2 endTilePosition = GetClickedGridPosition(currentMousePosition);

        // Clicking with the right mouse key is a useful debug function that gives the tile grid position of a clicked spot
        if (GameData.debugModeOn && Input.GetMouseButtonDown(1))
        {                        
            Vector2 clickedGridPosition = GetClickedGridPosition(currentMousePosition);            
            Debug.Log("The click hit grid position " + clickedGridPosition + ", which has coordinates " + gameController.GetTileWorldSpacePosition(clickedGridPosition));
        }

        if (Input.GetMouseButtonDown(0))
        {
            tileDragMovement = Vector2.zero;

            // Determine which tile has been clicked by using the tileGrid and the board top left corner position
            // NOTE: We'll need to flip the y coordinate since screen position y we want to use increases as we go downwards, but world position y increases as we go upwards.            
            Vector2 clickedPosition = new (Input.mousePosition.x, gameController.screenSize.y - Input.mousePosition.y); 

            // Figure out which grid square the click lands on                      
            Vector2 clickedTileGridPosition = GetClickedGridPosition(clickedPosition);

            // Highlight this square
            if (clickedTileGridPosition.x != -1 && gameController.tileGrid[(int)clickedTileGridPosition.x, (int)clickedTileGridPosition.y].tile != null)
            {
                AudioManager.instance.Play(Sound.TilePress);
                highlighter.gameObject.SetActive(true);
                highlighter.transform.position = gameController.tileGrid[(int)clickedTileGridPosition.x, (int)clickedTileGridPosition.y].tile.transform.position;
                dragStartPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition); // NOTE: This doesn't use flipped y coordinates, but it's only used for relative calculations with other non-flipped y coordinates,
                // and not for calculating absolute positions
                dragStartTileGridPosition = clickedTileGridPosition;
                markerArrow.transform.position = highlighter.transform.position;                
            }
        }

        if (Input.GetMouseButton(0))
        {                                    
            bool arrowIsHorizontal = true; // false means this is vertical, true means this is horizontal
            
            if (dragStartTileGridPosition == -Vector2.one || dragStartTileGridPosition == endTilePosition) // Don't draw the arrow if there's no valid drag action taking place
                arrowRenderer.size = Vector2.zero; //hide the arrow
            else
            {
                // The drag end tile position can't be outside the grid, so cap the end tile position if our mouse is outside the grid
                if (endTilePosition.x == -1)
                    endTilePosition.x = currentMousePosition.x < gameController.boardTopLeftCorner.x ? 0 : GameData.gridSize.x - 1;

                if (endTilePosition.y == -1)
                    endTilePosition.y = currentMousePosition.y < gameController.boardTopLeftCorner.y ? 0 : GameData.gridSize.y - 1;

                if (dragStartTileGridPosition.x != endTilePosition.x && dragStartTileGridPosition.y != endTilePosition.y) // We've dragged to a diagonal square, so determine the arrow direction by comparing x and y coordinates
                {
                    Vector2 draggedDistance = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - dragStartPoint;
                    arrowIsHorizontal = Mathf.Abs(draggedDistance.x) > Mathf.Abs(draggedDistance.y);
                }
                else if (dragStartTileGridPosition.x == endTilePosition.x) // We've dragged to a tile grid square that's on the same column
                    arrowIsHorizontal = false;
                else if (dragStartTileGridPosition.y == endTilePosition.y) // We've dragged to a tile grid square that's on the same row
                    arrowIsHorizontal = true;


                /*For drawing the arrow, we're changing the "size" variable of the SpriteRenderer, which has also had its draw mode set to "Sliced". This means that the body of the arrow (the long line) will
                * be the part that is stretched, and the head remains the same size. To make this work nicely, the arrow body should be something that can be extended in this way so that it still looks nice.
                * This border has been set for the "Arrow" sprite in the sprite editor.
                * Another option would be to draw the arrow in a textured mode, so that the body texture would be repeated multiple times before the head gets drawn. The body would have to fit the head seamlessly
                * regardless of the body tile position where it attaches to the head, though.
                */
                if (arrowIsHorizontal)
                {                    
                    arrowRenderer.size = new Vector2(Mathf.Abs(endTilePosition.x - dragStartTileGridPosition.x), 0.5f);

                    Vector3 newEulerAngles = markerArrow.transform.localEulerAngles;
                    newEulerAngles.z = endTilePosition.x < dragStartTileGridPosition.x ? 180 : 0;
                    markerArrow.transform.localEulerAngles = newEulerAngles;

                    tileDragMovement = new Vector2(endTilePosition.x - dragStartTileGridPosition.x, 0);
                }
                else // Same as the above if branch, but using the y coordinate instead of x, and also different angles
                {
                    arrowRenderer.size = new Vector2(Mathf.Abs(endTilePosition.y - dragStartTileGridPosition.y), 0.5f);

                    Vector3 newEulerAngles = markerArrow.transform.localEulerAngles;
                    newEulerAngles.z = endTilePosition.y < dragStartTileGridPosition.y ? 90 : 270;
                    markerArrow.transform.localEulerAngles = newEulerAngles;

                    tileDragMovement = new Vector2(0, endTilePosition.y - dragStartTileGridPosition.y);
                }
            }
        }
        else // Finger/mouse not held        
        {            
            arrowRenderer.size = Vector2.zero; // Hide the arrow
            highlighter.gameObject.SetActive(false);            
        }

        // Snapping a tile to its place after release
        if (Input.GetMouseButtonUp(0) && dragStartTileGridPosition != endTilePosition && dragStartTileGridPosition.x != -1)
        {
            //Debug.Log("Tile drag movement when released: " + tileDragMovement);
            AudioManager.instance.Play(Sound.TileSlide);

            int dragStartCol = (int)dragStartTileGridPosition.x;
            int dragStartRow = (int)dragStartTileGridPosition.y;
            GridSquare currentGridSquare;

            // Move the highlighted tile and all other tiles horizontally/vertically depending on the magnitude of the drag movement
            if ((int)tileDragMovement.x != 0)
            {
                int moveDir = (int)Mathf.Sign(tileDragMovement.x);

                // Starting from the last tile affected by the drag (the ending square of the drag), move backwards step by step and position the tiles correctly in between the drag start point and the last affected tile
                for (int i = (int)Mathf.Abs(tileDragMovement.x); i > 0; i--)
                {
                    currentGridSquare = gameController.tileGrid[dragStartCol + i * moveDir, dragStartRow];

                    if (currentGridSquare.tile == null)
                    {
                        // In order to move the tile to its new position, we'll also have to check all the tiles that are in between the drag start point and the end point
                        int freeSquareCol = dragStartCol + i * moveDir;

                        // Starting from one tile before the end tile, check backwards and move the tile as far from its starting point as we can 
                        for (int j = (int)Mathf.Abs(tileDragMovement.x) - 1; j >= 0; j--)
                        {
                            // If we find a tile along the way, we push it to the first free square
                            if (gameController.tileGrid[dragStartCol + j * moveDir, dragStartRow].tile != null)
                            {
                                gameController.tileGrid[freeSquareCol, dragStartRow].tile = gameController.tileGrid[dragStartCol + j * moveDir, dragStartRow].tile;
                                // NOTE: We're just snapping the tiles to correct positions now. If motion is wanted, we'll need to use Vector3.Lerp or similar function to interpolate between the start and end positions
                                // to create the motion.
                                gameController.tileGrid[freeSquareCol, dragStartRow].tile.transform.position = gameController.GetTileWorldSpacePosition(new Vector2(freeSquareCol, dragStartRow));
                                gameController.tileGrid[dragStartCol + j * moveDir, dragStartRow].tile = null;
                                freeSquareCol -= moveDir; // Update the new free square to be at the square that comes before the last free square                                
                            }
                        }
                        gameController.OnMovePlayed();
                        break;
                    }
                }
            }
            else if ((int)tileDragMovement.y != 0)
            {
                // This is basically the same as the above if (tileDragMovement.x != 0) branch, but we've swapped rows with columns and x with y. Even though there's a bit of copy-paste here, I felt that this is still cleaner to read than to create
                // a separate function for both horizontal and vertical versions, since that would require parametrization of the function and make it harder to read.

                int moveDir = (int)Mathf.Sign(tileDragMovement.y);

                for (int i = (int)Mathf.Abs(tileDragMovement.y); i > 0; i--)
                {
                    currentGridSquare = gameController.tileGrid[dragStartCol, dragStartRow + i * moveDir];

                    if (currentGridSquare.tile == null)
                    {
                        // In order to move the tile to its new position, we'll also have to check all the tiles that are in between the drag start point and the end point
                        int freeSquareRow = dragStartRow + i * moveDir;

                        // Starting from one tile before the end tile, check backwards and move the tile as far from its starting point as we can 
                        for (int j = (int)Mathf.Abs(tileDragMovement.y) - 1; j >= 0; j--)
                        {
                            // If we find a tile along the way, we push it to the first free square
                            if (gameController.tileGrid[dragStartCol, dragStartRow + j * moveDir].tile != null)
                            {                                
                                gameController.tileGrid[dragStartCol, freeSquareRow].tile = gameController.tileGrid[dragStartCol, dragStartRow + j * moveDir].tile;
                                // NOTE: We're just snapping the tiles to correct positions now. If motion is wanted, we'll need to use Vector3.Lerp or similar function to interpolate between the start and end positions
                                // to create the motion.
                                gameController.tileGrid[dragStartCol, freeSquareRow].tile.transform.position = gameController.GetTileWorldSpacePosition(new Vector2(dragStartCol, freeSquareRow));
                                gameController.tileGrid[dragStartCol, dragStartRow + j * moveDir].tile = null;
                                freeSquareRow -= moveDir; // Update the new free square to be at the square that comes before the last free square                                
                            }
                        }
                        gameController.OnMovePlayed();
                        break;
                    }
                }
            }            
        }
        else if (Input.GetMouseButtonUp(0)) // No tile movement at all, so just play the release sound
        {
            if (dragStartTileGridPosition != -Vector2.one) // Drag start point is defined
            {
                if (gameController.tileGrid[(int)dragStartTileGridPosition.x, (int)dragStartTileGridPosition.y].tile != null) // We've actually dragged a tile and not just started a drag by clicking a point that doesn't contain a tile
                    AudioManager.instance.Play(Sound.TileRelease);
            }
        }

        // Reset these whenever a finger/mouse press is released
        if (Input.GetMouseButtonUp(0))
        {
            dragStartTileGridPosition = -Vector2.one;
            tileDragMovement = Vector2.zero;
        }
    }

    // Converts the given Vector2 position into the index of the corresponding grid tile position, i.e. to format (column, row)    
    // Returns (-1, -1) if we've clicked a position that has no grid square, i.e. outside the grid
    Vector2 GetClickedGridPosition(Vector2 clickedPosition)
    {
        int clickedColumn = Mathf.FloorToInt((clickedPosition.x - gameController.boardTopLeftCorner.x) / gameController.tileSize);
        int clickedRow = Mathf.FloorToInt((clickedPosition.y - gameController.boardTopLeftCorner.y) / gameController.tileSize);

        // If we're outside the boundaries of the grid, the click can't have hit any tiles
        if (clickedColumn < 0 || clickedColumn >= gameController.tileGrid.GetLength(0)
            || clickedRow < 0 || clickedRow >= gameController.tileGrid.GetLength(1)) // We check the 2nd dimension just in case there are cases where width and height of the array are not the same                           
            return new Vector2(-1, -1); //signifies an undefined tile
        
        return new Vector2(clickedColumn, clickedRow);
    }
}

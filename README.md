# Tile-Based Puzzle Game Principles

This repository contains samples from an implementation of a **tile-based puzzle game**. Below is an overview of the main game controller class, which manages the core gameplay mechanics.

## Game Overview

The game uses an **m x n grid** to organize puzzle pieces. The playable puzzle area is the central portion of the grid, sized **(m-2) x (n-2)**. Players must rearrange the puzzle tiles to achieve specific goals by dragging them into the correct positions.

### Example Setup

- **Grid Size**: `5x5`
- **Puzzle Size**: `3x3` (center of the grid)

### Objective

1. **Reorganize Puzzle Pieces**:  
   Rearrange the shuffled tiles in the center of the grid to their original order or into any other valid configuration.
   
2. **Match Patterns**:  
   Ensure the patterns on the edges of adjacent tiles match.  
   - For example:
     - If Tile **A** at `(0,0)` has pattern **X** on its **right** edge, then Tile **B** at `(1,0)` must have the same pattern **X** on its **left** edge.

---

## Gameplay Mechanics

1. **Randomized Puzzle Arrangement**:  
   At the start of the game, the puzzle pieces are shuffled and placed in the middle of the grid.

2. **Drag and Drop**:  
   Players can drag tiles to change their positions.

3. **Pattern Matching**:  
   The game validates the player's solution based on the alignment of patterns on adjacent tiles.

---

The classes handle the logic for:

1. **Grid setup**  
2. **Puzzle shuffling**  
3. **Player interaction (dragging)**  
4. **Validation of the puzzle solution**

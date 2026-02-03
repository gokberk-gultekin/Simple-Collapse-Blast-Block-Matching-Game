# Simple Collapse / Blast Block Matching Game

![Gameplay](gameplay.gif)

# Specs
- The minimum number of same colored blocks to create a collapsible / blastable group is 2.

- Board can have 2 to 10 rows and 2 to
10 columns.

- Total number of block colors in a game can be varied between 1 to 6.

- Each color blocks have different icons based on the number of items in corresponding groups for easier recognition of bigger groups by player.

- Extra blocks fill vacant areas created at the outside of the board and drop from the top of the corresponding column.

- During a deadlock situation, the grid is shuffled with using Fisher-Yates algorithm until the deadlock is resolved, within a limited execution time and a limited number of attempts, to prevent infinit loop occurrences.

- Detecting groups of matching blocks: the grid is searched using a non-recursive Breadth-First Search Flood Fill algorithm to avoid recursion overhead.
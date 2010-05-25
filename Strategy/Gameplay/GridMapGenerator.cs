using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Strategy.Gameplay
{
    /// <summary>
    /// Generates random maps.
    /// </summary>
    public class GridMapGenerator
    {
        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        public GridMapGenerator() : this(new Random())
        {
        }

        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        /// <param name="random">The random number generator to use.</param>
        public GridMapGenerator(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// Generates a new map.
        /// </summary>
        /// <param name="numTerritories">The number of territories on the map.</param>
        /// <param name="territoriesPerPlayer">The number of territories per player.</param>
        /// <returns>The generated map.</returns>
        public GridMap Generate(int numTerritories, int territoriesPerPlayer)
        {
            GridTerritory[] territories = new GridTerritory[numTerritories];

            const int EXPANSION = TERRITORY_SIZE + 2 * TERRITORY_GAP_SIZE + 1;
            int terrRows = (int)Math.Floor(Math.Sqrt(numTerritories));
            int terrCols = (int)Math.Ceiling(Math.Sqrt(numTerritories));
            int gridRows = terrRows * EXPANSION;
            int gridCols = terrCols * EXPANSION;
            int[,] map = new int[gridRows, gridCols];

            PlayerId? owner = null;
            int numUnownedTerritories = numTerritories - territoriesPerPlayer * 4;
            int numAssignedTerritories = 0;

            // place the territories
            for (int t = 0; t < numTerritories; t++)
            {
                territories[t] = new GridTerritory();

                int row = (t / terrCols) * EXPANSION;
                int col = (t % terrCols) * EXPANSION;
                PlaceTerritory(map, row, col, territories[t]);

                // assign the next territories to the next player after reaching the quota
                numAssignedTerritories += 1;
                if (owner == null && numAssignedTerritories == numUnownedTerritories)
                {
                    owner = PlayerId.A;
                    numAssignedTerritories = 0;
                }
                else if (owner != null && numAssignedTerritories == territoriesPerPlayer)
                {
                    owner += 1;
                    numAssignedTerritories = 0;
                }
            }

            return new GridMap(territories);
        }

        /// <summary>
        /// Places a territory at a random location on the map. Returns true if
        /// a suitable location was found in a reasonable number of tries;
        /// otherwise, false.
        /// </summary>
        private void PlaceTerritory(int[,] map, int anchorRow, int anchorCol, GridTerritory territory)
        {
            bool[,] layout = GenerateTerritoryLayout();
            int row, col;

            // find a free location
            do
            {
                row = _random.Next(anchorRow - TERRITORY_SIZE / 2, anchorRow + TERRITORY_SIZE / 2);
                col = _random.Next(anchorCol - TERRITORY_SIZE / 2, anchorCol + TERRITORY_SIZE / 2);
            } while (!CanPlaceTerritoryAt(map, layout, row, col));

            // find the center of the territory
            territory.Location = new Point(
                col + layout.GetLength(1) / 2,
                row + layout.GetLength(0) / 2);

            // mark the area as used
            for (int r = row; r < row + layout.GetLength(0); r++)
            {
                for (int c = col; c < col + layout.GetLength(1); c++)
                {
                    if (layout[r - row, c - col])
                    {
                        map[r, c] = 1;

                        int rr = r; // need a copy to avoid aliasing
                        int cc = c;
                        territory.Area.Add(new Point(cc, rr));
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the territory can be placed at the given location
        /// without overlapping any territory already placed.
        /// </summary>
        private bool CanPlaceTerritoryAt(int[,] map, bool[,] territory, int row, int col)
        {
            for (int r = row - TERRITORY_GAP_SIZE; r <= row + territory.GetLength(0) + TERRITORY_GAP_SIZE; r++)
            {
                for (int c = col - TERRITORY_GAP_SIZE; c <= col + territory.GetLength(1) + TERRITORY_GAP_SIZE; c++)
                {
                    if (r < 0 || r >= map.GetLength(0) || c < 0 || c >= map.GetLength(1) || map[r, c] != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a random territory grid layout.
        /// </summary>
        private bool[,] GenerateTerritoryLayout()
        {
            const int BASE_START = TERRITORY_FRILL_SIZE;
            const int BASE_END = BASE_START + TERRITORY_BASE_SIZE;

            bool[,] layout = new bool[TERRITORY_SIZE, TERRITORY_SIZE];

            // fill in the center for the piece tiles
            for (int r = BASE_START; r < BASE_END; r++)
            {
                for (int c = BASE_START; c < BASE_END; c++)
                {
                    layout[r, c] = true;
                }
            }

            // add randomized decoration
            for (int d = 0; d < 5; d++)
            {
                int baseRow = _random.Next(BASE_START, BASE_END);
                int deltaRows = _random.Next(-TERRITORY_SIZE, TERRITORY_SIZE);
                int startRow = Math.Min(baseRow, baseRow + deltaRows);
                int endRow = Math.Max(baseRow, baseRow + deltaRows);

                int baseCol = _random.Next(BASE_START, BASE_END);
                int deltaCols = _random.Next(-TERRITORY_SIZE, TERRITORY_SIZE);
                int startCol = Math.Min(baseCol, baseCol + deltaCols);
                int endCol = Math.Max(baseCol, baseCol + deltaCols);

                for (int r = startRow; r <= endRow; r++)
                {
                    for (int c = startCol; c <= endCol; c++)
                    {
                        if (r >= 0 && r < TERRITORY_SIZE && c >= 0 && c < TERRITORY_SIZE)
                        {
                            layout[r, c] = true;
                        }
                    }
                }
            }

            return layout;
        }

        private Random _random;

        private const int TERRITORY_BASE_SIZE = 3;
        private const int TERRITORY_FRILL_SIZE = 1;
        private const int TERRITORY_GAP_SIZE = 1;
        private const int TERRITORY_SIZE = TERRITORY_BASE_SIZE + 2 * TERRITORY_FRILL_SIZE;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Strategy.Library;

namespace Strategy.Gameplay
{
    /// <summary>
    /// Generates random maps.
    /// </summary>
    public class MapGenerator
    {
        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        public MapGenerator() : this(new Random())
        {
        }

        /// <summary>
        /// Creates a new map generator.
        /// </summary>
        /// <param name="random">The random number generator to use.</param>
        public MapGenerator(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// Generates a new map.
        /// </summary>
        /// <param name="numTerritories">The number of territories on the map.</param>
        /// <param name="territoriesPerPlayer">The number of territories per player.</param>
        /// <returns>The generated map.</returns>
        public Map Generate(int numTerritories, int territoriesPerPlayer)
        {
            Territory[] territories = new Territory[numTerritories];

            int gridSize = (int)(Math.Sqrt(numTerritories) * TERRITORY_SIZE * 1.6);
            Territory[,] map = new Territory[gridSize, gridSize];

            // create and place the territories
        DoPlacement:
            for (int t = 0; t < numTerritories; t++)
            {
                territories[t] = new Territory();
                territories[t].Capacity = GenerateTerritoryCapacity();

                bool placed = PlaceTerritory(map, territories[t], t == 0);
                if (!placed)
                {
                    // generated a map where no further territories may be
                    // placed, so try again with a new configuration (ugh)
                    Array.Clear(map, 0, map.Length);
                    goto DoPlacement;
                }
            }

            // assign owners to the territories
            for (PlayerId p = PlayerId.A; p <= PlayerId.D; p++)
            {
                for (int i = 0; i < territoriesPerPlayer; i++)
                {
                    while (true)
                    {
                        int t = _random.Next(numTerritories);
                        if (territories[t].Owner == null)
                        {
                            territories[t].Owner = p;
                            break;
                        }
                    }
                }
            }

            return new Map(territories);
        }

        /// <summary>
        /// Places a territory at a random location on the map. Returns true if
        /// a suitable location was found in a reasonable number of tries;
        /// otherwise, false.
        /// </summary>
        private bool PlaceTerritory(Territory[,] map, Territory territory, bool firstPlacement)
        {
            const int MAX_PLACE_TRIES = 100;

            bool[,] layout = GenerateTerritoryLayout();
            int row = 1, col = 1;
            int tries;

            // find a free location
            for (tries = 0; !PlaceTerritoryAt(map, territory, layout, row, col, firstPlacement) && tries < MAX_PLACE_TRIES; tries++)
            {
                row = _random.Next(2, map.GetLength(0) - layout.GetLength(0) - 2);
                col = _random.Next(2, map.GetLength(1) - layout.GetLength(1) - 2);
            }
            if (tries >= MAX_PLACE_TRIES)
            {
                return false;
            }

            // find the center of the territory
            territory.Location = new Cell(
                row + layout.GetLength(0) / 2,
                col + layout.GetLength(1) / 2);

            // mark the area as used
            for (int r = row; r < row + layout.GetLength(0); r++)
            {
                for (int c = col; c < col + layout.GetLength(1); c++)
                {
                    if (layout[r - row, c - col])
                    {
                        map[r, c] = territory;
                        territory.Area.Add(new Cell(r, c));
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the territory can be placed at the given location
        /// touching at least one other territory and without overlapping any
        /// territory already placed; otherwise, false.
        /// </summary>
        private bool PlaceTerritoryAt(Territory[,] map, Territory territory, bool[,] layout, int row, int col, bool firstPlacement)
        {
            int tr = layout.GetLength(0);
            int tc = layout.GetLength(1);

            // check for overlap
            for (int r = row - TERRITORY_GAP_SIZE; r < row + tr + TERRITORY_GAP_SIZE; r++)
            {
                for (int c = col - TERRITORY_GAP_SIZE; c < col + tc + TERRITORY_GAP_SIZE; c++)
                {
                    if (map[r, c] != null)
                    {
                        return false;
                    }
                }
            }

            // check for connectedness
            if (!firstPlacement)
            {
                List<Territory> neighbors = new List<Territory>(8);
                for (int r = 0; r < tr; r++)
                {
                    // check to the left
                    if (map[row + r, col - TERRITORY_GAP_SIZE - 1] != null && layout[r, 0])
                    {
                        neighbors.Add(map[row + r, col - TERRITORY_GAP_SIZE - 1]);
                    }
                    // check to the right
                    if (map[row + r, col + tc + TERRITORY_GAP_SIZE] != null && layout[r, tc - 1])
                    {
                        neighbors.Add(map[row + r, col + tc + TERRITORY_GAP_SIZE]);
                    }
                }
                for (int c = 0; c < tc; c++)
                {
                    // check above
                    if (map[row - TERRITORY_GAP_SIZE - 1, col + c] != null && layout[0, c])
                    {
                        neighbors.Add(map[row - TERRITORY_GAP_SIZE - 1, col + c]);
                    }
                    // check below
                    if (map[row + tr + TERRITORY_GAP_SIZE, col + c] != null && layout[tr - 1, c])
                    {
                        neighbors.Add(map[row + tr + TERRITORY_GAP_SIZE, col + c]);
                    }
                }
                // track the neighbors
                int count = 0;
                foreach (Territory neighbor in neighbors.Distinct())
                {
                    territory.Neighbors.Add(neighbor);
                    neighbor.Neighbors.Add(territory);
                    count += 1;
                }
                return count > 0;
            }
            else
            {
                return true; // first territory has nothing to connect to
            }
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

        /// <summary>
        /// Generates a random capacity for a territory.
        /// </summary>
        private int GenerateTerritoryCapacity()
        {
            double rand = _random.NextDouble();
            if (rand <= 0.3)
            {
                return 5;
            }
            else if (rand <= 0.9)
            {
                return 7;
            }
            else
            {
                return 9;
            }
        }

        private Random _random;

        private const int TERRITORY_BASE_SIZE = 3;
        private const int TERRITORY_FRILL_SIZE = 1;
        private const int TERRITORY_GAP_SIZE = 1;
        private const int TERRITORY_SIZE = TERRITORY_BASE_SIZE + 2 * TERRITORY_FRILL_SIZE;
    }
}

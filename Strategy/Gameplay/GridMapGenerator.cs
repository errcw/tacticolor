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

            int terrRows = (int)Math.Floor(Math.Sqrt(numTerritories));
            int terrCols = (int)Math.Ceiling(Math.Sqrt(numTerritories));
            int gridRows = terrRows * TERRITORY_SIZE * 2;
            int gridCols = terrCols * TERRITORY_SIZE * 2;
            GridTerritory[,] map = new GridTerritory[gridRows, gridCols];

            // create and place the territories
            for (int t = 0; t < numTerritories; t++)
            {
                territories[t] = new GridTerritory();
                PlaceTerritory(map, t == 0, territories[t]);
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

            // connect the territories
            /*foreach (GridTerritory ta in territories)
            {
                // find all the candidate territories
                List<GridTerritory> potentialNeighbors = new List<GridTerritory>(numTerritories);
                foreach (GridTerritory tb in territories)
                {
                    if (CanConnectTerritoryTo(map, ta, tb))
                    {
                        potentialNeighbors.Add(tb);
                    }
                }
                //System.Diagnostics.Debug.Assert(potentialNeighbors.Count + ta.Adjacent.Count >= CONNECTION_MIN_NUM);
                // add the minimum number of connections
                while (ta.Adjacent.Count < CONNECTION_MIN_NUM)
                {
                    for (int i = 0; i < potentialNeighbors.Count; i++)
                    {
                        GridTerritory tb = potentialNeighbors[i];
                        if (tb == null)
                        {
                            continue;
                        }
                        if (_random.NextDouble() < CONNECTION_CHANCE)
                        {
                            ta.Adjacent.Add(tb);
                            tb.Adjacent.Add(ta);
                            potentialNeighbors[i] = null;
                        }
                    }
                }
            }*/

            return new GridMap(territories);
        }

        /// <summary>
        /// Places a territory at a random location on the map. Returns true if
        /// a suitable location was found in a reasonable number of tries;
        /// otherwise, false.
        /// </summary>
        private void PlaceTerritory(GridTerritory[,] map, bool first, GridTerritory territory)
        {
            bool[,] layout = GenerateTerritoryLayout();
            int row, col;

            // find a free location
            do
            {
                row = _random.Next(2, map.GetLength(0) - layout.GetLength(0) - 2);
                col = _random.Next(2, map.GetLength(1) - layout.GetLength(1) - 2);
            } while (!CanPlaceTerritoryAt(map, layout, row, col, first, territory));

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
                        map[r, c] = territory;

                        int rr = r; // need a copy to avoid aliasing
                        int cc = c;
                        territory.Area.Add(new Point(c, r));
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the territory can be placed at the given location
        /// without overlapping any territory already placed; otherwise, false.
        /// </summary>
        private bool CanPlaceTerritoryAt(GridTerritory[,] map, bool[,] territory, int row, int col, bool first, GridTerritory t)
        {
            // check for overlap
            for (int r = row - TERRITORY_GAP_SIZE; r < row + territory.GetLength(0) + TERRITORY_GAP_SIZE; r++)
            {
                for (int c = col - TERRITORY_GAP_SIZE; c < col + territory.GetLength(1) + TERRITORY_GAP_SIZE; c++)
                {
                    if (r < 0 || r >= map.GetLength(0) || c < 0 || c >= map.GetLength(1) || map[r, c] != null)
                    {
                        return false;
                    }
                }
            }

            // check for connectedness
            if (!first)
            {
                List<GridTerritory> neighbors = new List<GridTerritory>(8);
                for (int r = 0; r < territory.GetLength(0); r++)
                {
                    // check to the left
                    if (map[row + r, col - TERRITORY_GAP_SIZE - 1] != null && territory[r, 0])
                    {
                        neighbors.Add(map[row + r, col - TERRITORY_GAP_SIZE - 1]);
                    }
                    // check to the right
                    if (map[row + r, col + territory.GetLength(1) + TERRITORY_GAP_SIZE] != null && territory[r, territory.GetLength(1) - 1])
                    {
                        neighbors.Add(map[row + r, col + territory.GetLength(1) + TERRITORY_GAP_SIZE]);
                    }
                }
                for (int c = 0; c < territory.GetLength(1); c++)
                {
                    // check above
                    if (map[row - 1 - TERRITORY_GAP_SIZE, col + c] != null && territory[0, c])
                    {
                        neighbors.Add(map[row - 1 - TERRITORY_GAP_SIZE, col + c]);
                    }
                    // check below
                    if (map[row + territory.GetLength(0) + TERRITORY_GAP_SIZE, col + c] != null && territory[territory.GetLength(0) - 1, c])
                    {
                        neighbors.Add(map[row + territory.GetLength(0) + TERRITORY_GAP_SIZE, col + c]);
                    }
                }
                int count = 0;
                foreach (GridTerritory tt in neighbors.Distinct())
                {
                    t.Adjacent.Add(tt);
                    tt.Adjacent.Add(t);
                    count += 1;
                }
                return count > 0;
            }
            else
            {
                return true; // first territory has no connections
            }
        }

        /// <summary>
        /// Returns true if the two territories can be connected without the
        /// connection crossing another territory; otherwise, false.
        /// </summary>
        private bool CanConnectTerritoryTo(GridTerritory[,] map, GridTerritory src, GridTerritory dst)
        {
            // no self-connections
            if (src == dst)
            {
                return false;
            }
            // no duplicate connections
            if (src.Adjacent.Contains(dst) || dst.Adjacent.Contains(src))
            {
                return false;
            }
            // no far connections
            float d2 = (src.Location.X - dst.Location.X) * (src.Location.X - dst.Location.X) +
                       (src.Location.Y - dst.Location.Y) * (src.Location.Y - dst.Location.Y);
            if (d2 > CONNECTION_MAX_DISTANCE_2)
            {
                return false;
            }
            // no non-planar connections
            foreach (Point point in BresenhamIterator.GetPointsOnLine(src.Location.X, src.Location.Y, dst.Location.X, dst.Location.Y))
            {
                GridTerritory t = map[point.Y, point.X];
                if (t != null && t != src && t != dst)
                {
                    return false;
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
        private const int CONNECTION_MIN_NUM = 0;
        private const int CONNECTION_MAX_DISTANCE_2 = 15 * 15;
        private const double CONNECTION_CHANCE = 0.5;
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Strategy.Library;

namespace Strategy.Gameplay
{
    /// <summary>
    /// Enumerates the possible map sizes.
    /// </summary>
    public enum MapSize
    {
        Tiny = 8,
        Small = 12,
        Normal = 16,
        Large = 20
    }

    /// <summary>
    /// Enumerates the possible map types.
    /// </summary>
    public enum MapType
    {
        LandRush,
        Filled
    }

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
        /// <param name="mapType">The type of map to generate.</param>
        /// <param name="mapSize">The size of map to generate.</param>
        /// <param name="numPlayers">The number of players in the match.</param>
        /// <returns>The generated map.</returns>
        public Map Generate(MapType mapType, MapSize mapSize, int numPlayers)
        {
            int numTerritories = (int)mapSize;
            int territoriesPerPlayer = (mapType == MapType.LandRush ? 1 : numTerritories / numPlayers);
            int piecesPerPlayer = territoriesPerPlayer * 2 + 2;
            return Generate(numTerritories, numPlayers, territoriesPerPlayer, piecesPerPlayer);
        }

        /// <summary>
        /// Generates a new map.
        /// </summary>
        /// <param name="numTerritories">The number of territories on the map.</param>
        /// <param name="numPlayers">The number of players to assign.</param>
        /// <param name="territoriesPerPlayer">The number of territories per player.</param>
        /// <param name="piecesPerPlayer">The number of pieces per player.</param>
        /// <returns>The generated map.</returns>
        public Map Generate(int numTerritories, int numPlayers, int territoriesPerPlayer, int piecesPerPlayer)
        {
            Territory[] territories = new Territory[numTerritories];

            int gridSize = (int)(Math.Sqrt(numTerritories) * TerritorySize * MapExpansionFactor);
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

            // assign owners and pieces to the territories
            int piecesPerTerritory = piecesPerPlayer / territoriesPerPlayer;
            int piecesPerTerritoryLeftover = piecesPerPlayer % territoriesPerPlayer;
            for (int p = 0; p < numPlayers; p++)
            {
                PlayerId player = (PlayerId)p;
                for (int i = 0; i < territoriesPerPlayer; i++)
                {
                    while (true)
                    {
                        int t = _random.Next(numTerritories);
                        if (territories[t].Owner == null)
                        {
                            int occupancy = piecesPerTerritory;
                            if (i == 0)
                            {
                                occupancy += piecesPerTerritoryLeftover;
                            }
                            for (int j = 0; j < occupancy; j++)
                            {
                                territories[t].Pieces.Add(new Piece(player, true));
                            }
                            territories[t].Owner = player;
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
                row = _random.Next(3, map.GetLength(0) - layout.GetLength(0) - 3);
                col = _random.Next(3, map.GetLength(1) - layout.GetLength(1) - 3);
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
            for (int r = row - TerritoryGapSize; r < row + tr + TerritoryGapSize; r++)
            {
                for (int c = col - TerritoryGapSize; c < col + tc + TerritoryGapSize; c++)
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
                    for (int d = 0; d < TerritoryConnectionDistance; d++)
                    {
                        // check to the left
                        if (map[row + r, col - TerritoryGapSize - 1 - d] != null && layout[r, 0])
                        {
                            neighbors.Add(map[row + r, col - TerritoryGapSize - 1 - d]);
                        }
                        // check to the right
                        if (map[row + r, col + tc + TerritoryGapSize + d] != null && layout[r, tc - 1])
                        {
                            neighbors.Add(map[row + r, col + tc + TerritoryGapSize + d]);
                        }
                    }
                }
                for (int c = 0; c < tc; c++)
                {
                    for (int d = 0; d < TerritoryConnectionDistance; d++)
                    {
                        // check above
                        if (map[row - TerritoryGapSize - 1 - d, col + c] != null && layout[0, c])
                        {
                            neighbors.Add(map[row - TerritoryGapSize - 1 - d, col + c]);
                        }
                        // check below
                        if (map[row + tr + TerritoryGapSize + d, col + c] != null && layout[tr - 1, c])
                        {
                            neighbors.Add(map[row + tr + TerritoryGapSize + d, col + c]);
                        }
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
            const int BaseStart = TerritoryFrillSize;
            const int BaseEnd = BaseStart + TerritoryBaseSize;

            bool[,] layout = new bool[TerritorySize, TerritorySize];

            // fill in the center for the piece tiles
            for (int r = BaseStart; r < BaseEnd; r++)
            {
                for (int c = BaseStart; c < BaseEnd; c++)
                {
                    layout[r, c] = true;
                }
            }

            // add randomized decoration
            int frills = 0;
            for (int d = 0; d < 5 || frills < TerritoryFrillMinCount; d++)
            {
                int baseRow = _random.Next(BaseStart, BaseEnd);
                int deltaRows = _random.Next(-TerritorySize, TerritorySize);
                int startRow = Math.Min(baseRow, baseRow + deltaRows);
                int endRow = Math.Max(baseRow, baseRow + deltaRows);

                int baseCol = _random.Next(BaseStart, BaseEnd);
                int deltaCols = _random.Next(-TerritorySize, TerritorySize);
                int startCol = Math.Min(baseCol, baseCol + deltaCols);
                int endCol = Math.Max(baseCol, baseCol + deltaCols);

                for (int r = startRow; r <= endRow; r++)
                {
                    for (int c = startCol; c <= endCol; c++)
                    {
                        if (r >= 0 && r < TerritorySize && c >= 0 && c < TerritorySize)
                        {
                            if (!layout[r, c])
                            {
                                layout[r, c] = true;
                                frills += 1;
                            }
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

        private const float MapExpansionFactor = 1.75f;
        private const int TerritoryBaseSize = 3;
        private const int TerritoryFrillSize = 1;
        private const int TerritoryFrillMinCount = 4;
        private const int TerritoryGapSize = 1;
        private const int TerritoryConnectionDistance = 2;
        private const int TerritorySize = TerritoryBaseSize + 2 * TerritoryFrillSize;
    }
}

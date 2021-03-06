﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

namespace Strategy.Gameplay
{
    /// <summary>
    /// A single grid cell in the map.
    /// </summary>
    public struct Cell
    {
        /// <summary>
        /// The row of the cell.
        /// </summary>
        public int Row;

        /// <summary>
        /// The column of the cell.
        /// </summary>
        public int Col;

        /// <summary>
        /// Creates a new grid cell.
        /// </summary>
        /// <param name="row">The row of the cell.</param>
        /// <param name="col">The col of the cell.</param>
        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    /// <summary>
    /// A contiguous region on a map.
    /// </summary>
    public class Territory
    {
        /// <summary>
        /// The center of the territory.
        /// </summary>
        public Cell Location { get; set; }

        /// <summary>
        /// The grid cells occupied by this territory.
        /// </summary>
        public ICollection<Cell> Area { get; private set; }

        /// <summary>
        /// The territories adjacent to this territory.
        /// </summary>
        public ICollection<Territory> Neighbors { get; private set; }

        /// <summary>
        /// The maximum number of pieces that may be in the territory.
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// The pieces currently in the territory.
        /// </summary>
        public ICollection<Piece> Pieces { get; private set; }

        /// <summary>
        /// The player owning this territory (null for unowned).
        /// </summary>
        public PlayerId? Owner { get; set; }

        /// <summary>
        /// The cooldown time remaining for this territory.
        /// </summary>
        public int Cooldown { get; set; }

        /// <summary>
        /// Creates a new territory.
        /// </summary>
        public Territory()
        {
            Area = new List<Cell>();
            Neighbors = new List<Territory>();
            Pieces = new List<Piece>();
        }
    }

    /// <summary>
    /// Represents a map.
    /// </summary>
    public class Map
    {
        /// <summary>
        /// The territories contained in this map.
        /// </summary>
        public ICollection<Territory> Territories { get; private set; }

        /// <summary>
        /// Creates a new map with a fixed set of territories.
        /// </summary>
        /// <param name="territories">The territories on the map.</param>
        public Map(Territory[] territories)
        {
            Territories = territories;
        }

        /// <summary>
        /// Returns the territory at the given location, or null if no such territory exists.
        /// </summary>
        public Territory GetTerritoryAt(Cell cell)
        {
            foreach (Territory territory in Territories)
            {
                if (territory.Location.Row == cell.Row && territory.Location.Col == cell.Col)
                {
                    return territory;
                }
            }
            return null;
        }
    }
}

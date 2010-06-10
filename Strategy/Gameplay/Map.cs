using System;
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
        public IList<Cell> Area { get; private set; }

        /// <summary>
        /// The territories adjacent to this territory.
        /// </summary>
        public IList<Territory> Adjacent { get; private set; }

        /// <summary>
        /// The player owning this territory.
        /// </summary>
        public PlayerId? Owner { get; set; }

        /// <summary>
        /// Creates a new territory.
        /// </summary>
        public Territory()
        {
            Area = new List<Cell>();
            Adjacent = new List<Territory>();
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
        public IEnumerable<Territory> Territories { get; private set; }

        /// <summary>
        /// The minimum row and column in this map.
        /// </summary>
        public Cell MinExtent { get; private set; }

        /// <summary>
        /// The maximum row and column in this map.
        /// </summary>
        public Cell MaxExtent { get; private set; }

        /// <summary>
        /// Creates a new map with a fixed set of territories.
        /// </summary>
        /// <param name="territories">The territories on the map.</param>
        public Map(Territory[] territories)
        {
            Territories = territories;
            CalculateExtents();
        }

        /// <summary>
        /// Finds the extents of this map.
        /// </summary>
        private void CalculateExtents()
        {
            int minRow = int.MaxValue, minCol = int.MaxValue;
            int maxRow = int.MinValue, maxCol = int.MinValue;
            foreach (Territory t in Territories)
            {
                foreach (Cell c in t.Area)
                {
                    if (c.Row < minRow)
                    {
                        minRow = c.Row;
                    }
                    if (c.Row > maxRow)
                    {
                        maxRow = c.Row;
                    }
                    if (c.Col < minCol)
                    {
                        minCol = c.Col;
                    }
                    if (c.Col > maxCol)
                    {
                        maxCol = c.Col;
                    }
                }
            }
            MinExtent = new Cell(minRow, minCol);
            MaxExtent = new Cell(maxRow, maxCol);
        }

        /// <summary>
        /// Iterates over all the territory area in the map.
        /// </summary>
        private IEnumerable<Cell> GetArea()
        {
            foreach (Territory t in Territories)
            {
                foreach (Cell c in t.Area)
                {
                    yield return c;
                }
            }
            yield break;
        }
    }
}
